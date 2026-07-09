using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 弹幕显示组件。
/// 
/// 该组件只负责弹幕的“显示效果”：
/// 1. 接收已经解析好的 <see cref="InteractiveBarrageMessage"/>。
/// 2. 把消息放入等待队列。
/// 3. 按轨道从屏幕右侧生成弹幕。
/// 4. 每帧向左移动。
/// 5. 弹幕离屏后回收复用。
/// 
/// 它不负责 WebSocket 连接、不负责服务器地址、不负责二维码，这些由
/// <see cref="InteractiveBarrageClient"/> 和 <see cref="QrCodeManager"/> 负责。
/// 
/// itemTemplate 使用 Graphic 类型，是为了同时兼容：
/// - TextMeshProUGUI：显示效果好，但需要完整 TMP 字库。
/// - UnityEngine.UI.Text：中文兼容性更直接，适合避免 TMP 缺字。
/// </summary>
public class BarrageDisplay : MonoBehaviour
{
    [Header("UI 引用")]
    /// <summary>
    /// 弹幕运动区域的根节点。
    /// 所有弹幕项都会挂到这个节点下，并使用 RectTransform.anchoredPosition 做移动。
    /// 如果没有绑定，会优先使用当前物体的 RectTransform；仍不存在时会自动创建 Canvas 和根节点。
    /// </summary>
    [SerializeField] private RectTransform barrageRoot;

    /// <summary>
    /// 弹幕项模板。
    /// 模板可以是 TMP，也可以是原生 Text。模板本体会被隐藏，只作为克隆源。
    /// </summary>
    [SerializeField] private Graphic itemTemplate;

    [Header("布局")]
    /// <summary>弹幕轨道数量。轨道越多，同屏弹幕越密集。</summary>
    [SerializeField] private int laneCount = 8;

    /// <summary>单条轨道的基础高度，用于纵向排布。</summary>
    [SerializeField] private float laneHeight = 48f;

    /// <summary>轨道之间的额外间距，防止上下弹幕文本或描边重叠。</summary>
    [SerializeField] private float laneVerticalGap = 8f;

    /// <summary>弹幕区域顶部留白，避免第一行贴到屏幕边缘。</summary>
    [SerializeField] private float paddingTop = 32f;

    /// <summary>同一轨道上两条弹幕之间的最小水平间距。</summary>
    [SerializeField] private float minHorizontalGap = 80f;

    /// <summary>等待显示的消息队列上限。超过后丢弃最早的未显示消息，避免高并发拖垮 UI。</summary>
    [SerializeField] private int maxPendingMessages = 200;

    /// <summary>同屏正在移动的弹幕上限。用于限制最坏情况下的 UI 对象数量。</summary>
    [SerializeField] private int maxActiveItems = 40;

    [Header("动画")]
    /// <summary>弹幕移动速度，单位是 UI 像素/秒。</summary>
    [SerializeField] private float moveSpeed = 420f;

    /// <summary>全局弹幕生成间隔，避免同一帧创建过多 UI 对象。</summary>
    [SerializeField] private float spawnInterval = 0.12f;

    /// <summary>TMP 模式下使用的字体资产。</summary>
    [SerializeField] private TMP_FontAsset barrageFontAsset;

    /// <summary>TMP 字体资源路径，位于 Resources 下，用于未手动配置字体时兜底加载。</summary>
    [SerializeField] private string defaultFontResourcePath = "Fonts/simhei SDF";

    /// <summary>原生 Text 模式下使用的字体。未配置时会使用模板字体，最后兜底 Arial。</summary>
    [SerializeField] private Font legacyTextFont;

    /// <summary>弹幕字号，TMP 和原生 Text 共用。</summary>
    [SerializeField] private float fontSize = 30f;

    /// <summary>弹幕文字颜色。</summary>
    [SerializeField] private Color textColor = Color.white;

    /// <summary>弹幕描边颜色。TMP 用自身描边属性，原生 Text 用 Outline 组件。</summary>
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.75f);

    /// <summary>弹幕描边宽度。原生 Text 会转换为 Outline.effectDistance。</summary>
    [SerializeField] private float outlineWidth = 0.18f;

    /// <summary>收到但还没有生成到屏幕上的弹幕队列。</summary>
    private readonly Queue<InteractiveBarrageMessage> pendingMessages = new Queue<InteractiveBarrageMessage>();

    /// <summary>离屏后可复用的弹幕 UI 项池。</summary>
    private readonly Queue<BarrageTextItem> itemPool = new Queue<BarrageTextItem>();

    /// <summary>当前正在屏幕上移动的弹幕项。</summary>
    private readonly List<ActiveBarrageItem> activeItems = new List<ActiveBarrageItem>();

    /// <summary>每条轨道下一次允许生成弹幕的时间点。</summary>
    private float[] nextLaneAvailableTimes;

    /// <summary>全局下一次允许生成弹幕的时间点。</summary>
    private float nextSpawnTime;

    /// <summary>
    /// 屏幕上正在移动的一条弹幕。
    /// 保存 RectTransform 是为了每帧移动；保存 EndX 是为了判断何时完全离屏。
    /// </summary>
    private class ActiveBarrageItem
    {
        public BarrageTextItem TextItem;
        public RectTransform RectTransform;
        public float EndX;
    }

    /// <summary>
    /// 对 TMP 和原生 Text 的统一封装。
    /// 业务逻辑统一操作 BarrageTextItem，内部根据 TmpText 或 LegacyText 决定具体 API。
    /// </summary>
    private class BarrageTextItem
    {
        public GameObject GameObject;
        public RectTransform RectTransform;
        public TMP_Text TmpText;
        public Text LegacyText;

        public bool IsValid => GameObject && RectTransform && (TmpText || LegacyText);
    }

    private void Awake()
    {
        EnsureRoot();
        EnsureTemplate();
        EnsureLaneState();
    }

    private void Update()
    {
        // laneCount 可能运行时在 Inspector 中调整，因此轻量检查轨道数组长度。
        EnsureLaneState();
        SpawnPendingMessages();
        MoveActiveItems();
    }

    /// <summary>
    /// 把一条弹幕加入等待显示队列。
    /// 队列满时丢弃最早未显示的消息，避免大量手机同时发送时造成 UI 长时间积压。
    /// </summary>
    public void EnqueueBarrage(InteractiveBarrageMessage message)
    {
        if (message == null || string.IsNullOrEmpty(message.content))
        {
            return;
        }

        while (pendingMessages.Count >= maxPendingMessages)
        {
            pendingMessages.Dequeue();
        }

        pendingMessages.Enqueue(message);
    }

    /// <summary>
    /// 清空等待显示和正在显示的弹幕。
    /// 服务器发送 clear 消息或调试时可以调用。
    /// </summary>
    public void Clear()
    {
        pendingMessages.Clear();

        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            RecycleItem(activeItems[i]);
        }

        activeItems.Clear();

        if (nextLaneAvailableTimes != null)
        {
            for (int i = 0; i < nextLaneAvailableTimes.Length; i++)
            {
                nextLaneAvailableTimes[i] = 0f;
            }
        }
    }

    /// <summary>
    /// 尝试从等待队列中取出一条消息并生成到屏幕上。
    /// 该方法会同时检查全局生成间隔、同屏数量上限和轨道可用时间。
    /// </summary>
    private void SpawnPendingMessages()
    {
        if (pendingMessages.Count == 0 || activeItems.Count >= maxActiveItems || Time.time < nextSpawnTime)
        {
            return;
        }

        int lane = FindAvailableLane();
        if (lane < 0)
        {
            return;
        }

        InteractiveBarrageMessage message = pendingMessages.Dequeue();
        BarrageTextItem textItem = GetItem();
        if (textItem == null || !textItem.IsValid)
        {
            return;
        }

        RectTransform rectTransform = textItem.RectTransform;

        SetItemText(textItem, FormatMessage(message));
        ApplyTextStyle(textItem);

        // 根据文本实际内容计算尺寸，避免长弹幕被裁剪。
        Vector2 preferredValues = GetPreferredValues(textItem);
        float width = Mathf.Max(120f, preferredValues.x + 48f);
        float height = Mathf.Max(laneHeight, preferredValues.y + 12f);
        rectTransform.sizeDelta = new Vector2(width, height);

        float rootWidth = GetRootWidth();
        float rootHeight = GetRootHeight();

        // 从屏幕右侧外部进入，从屏幕左侧外部离开，保证完整显示。
        float startX = rootWidth * 0.5f + width * 0.5f;
        float endX = -rootWidth * 0.5f - width * 0.5f;
        float laneStep = laneHeight + laneVerticalGap;
        float y = rootHeight * 0.5f - paddingTop - laneHeight * 0.5f - lane * laneStep;

        rectTransform.anchoredPosition = new Vector2(startX, y);
        textItem.GameObject.SetActive(true);

        activeItems.Add(new ActiveBarrageItem
        {
            TextItem = textItem,
            RectTransform = rectTransform,
            EndX = endX
        });

        // 同一轨道下一条弹幕需要等当前弹幕移动出安全距离后才能生成。
        float spacingTime = (width + minHorizontalGap) / Mathf.Max(1f, moveSpeed);
        nextLaneAvailableTimes[lane] = Time.time + spacingTime;
        nextSpawnTime = Time.time + spawnInterval;
    }

    /// <summary>
    /// 每帧移动所有已激活弹幕，并回收已经完全离开屏幕左侧的项。
    /// </summary>
    private void MoveActiveItems()
    {
        float delta = moveSpeed * Time.deltaTime;
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            ActiveBarrageItem item = activeItems[i];
            Vector2 position = item.RectTransform.anchoredPosition;
            position.x -= delta;
            item.RectTransform.anchoredPosition = position;

            if (position.x <= item.EndX)
            {
                activeItems.RemoveAt(i);
                RecycleItem(item);
            }
        }
    }

    /// <summary>
    /// 寻找当前可以生成新弹幕的轨道。
    /// 优先选择已经完全可用的轨道；如果最近一条轨道很快就可用，允许轻微提前。
    /// </summary>
    private int FindAvailableLane()
    {
        float now = Time.time;
        int bestLane = -1;
        float bestTime = float.MaxValue;

        for (int i = 0; i < nextLaneAvailableTimes.Length; i++)
        {
            if (nextLaneAvailableTimes[i] <= now)
            {
                return i;
            }

            if (nextLaneAvailableTimes[i] < bestTime)
            {
                bestTime = nextLaneAvailableTimes[i];
                bestLane = i;
            }
        }

        return bestTime - now < 0.25f ? bestLane : -1;
    }

    /// <summary>
    /// 从本地池获取弹幕 UI 项；池为空时克隆模板。
    /// </summary>
    private BarrageTextItem GetItem()
    {
        if (itemPool.Count > 0)
        {
            return itemPool.Dequeue();
        }

        Graphic text = Instantiate(itemTemplate, barrageRoot);
        text.name = "BarrageItem";
        return CreateTextItem(text);
    }

    /// <summary>
    /// 回收离屏弹幕项。这里只隐藏并入池，不销毁 GameObject。
    /// </summary>
    private void RecycleItem(ActiveBarrageItem item)
    {
        if (item == null || item.TextItem == null || !item.TextItem.IsValid)
        {
            return;
        }

        item.TextItem.GameObject.SetActive(false);
        itemPool.Enqueue(item.TextItem);
    }

    /// <summary>
    /// 当前弹幕只显示内容本身，不显示用户身份、昵称等前缀。
    /// </summary>
    private string FormatMessage(InteractiveBarrageMessage message)
    {
        return message.content;
    }

    /// <summary>
    /// 确保弹幕根节点存在。
    /// 这让组件即使没有完整 prefab 绑定，也能在调试环境中尽量自恢复。
    /// </summary>
    private void EnsureRoot()
    {
        if (barrageRoot)
        {
            return;
        }

        RectTransform selfRect = transform as RectTransform;
        if (selfRect)
        {
            barrageRoot = selfRect;
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            GameObject canvasObject = new GameObject("BarrageCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject rootObject = new GameObject("BarrageRoot");
        rootObject.transform.SetParent(canvas.transform, false);
        barrageRoot = rootObject.AddComponent<RectTransform>();
        barrageRoot.anchorMin = Vector2.zero;
        barrageRoot.anchorMax = Vector2.one;
        barrageRoot.offsetMin = Vector2.zero;
        barrageRoot.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 确保弹幕模板存在。
    /// 如果没有配置模板，默认创建原生 Text 模板，降低 TMP 字库缺字导致中文显示异常的风险。
    /// </summary>
    private void EnsureTemplate()
    {
        if (itemTemplate)
        {
            itemTemplate.gameObject.SetActive(false);
            ApplyTextStyle(CreateTextItem(itemTemplate));
            return;
        }

        GameObject templateObject = new GameObject("BarrageItemTemplate");
        templateObject.transform.SetParent(barrageRoot, false);
        itemTemplate = templateObject.AddComponent<Text>();
        itemTemplate.raycastTarget = false;
        ApplyTextStyle(CreateTextItem(itemTemplate));

        RectTransform rectTransform = itemTemplate.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(400f, laneHeight);

        itemTemplate.gameObject.SetActive(false);
    }

    /// <summary>
    /// 把 Graphic 模板或实例包装成 BarrageTextItem。
    /// 同一个对象通常只会有 TMP 或 Text 其中一种组件，但这里同时检查以提高 prefab 兼容性。
    /// </summary>
    private BarrageTextItem CreateTextItem(Graphic graphic)
    {
        if (!graphic)
        {
            return null;
        }

        return new BarrageTextItem
        {
            GameObject = graphic.gameObject,
            RectTransform = graphic.rectTransform,
            TmpText = graphic.GetComponent<TMP_Text>(),
            LegacyText = graphic.GetComponent<Text>()
        };
    }

    /// <summary>
    /// 给文本项写入弹幕内容。
    /// </summary>
    private void SetItemText(BarrageTextItem item, string content)
    {
        if (item == null)
        {
            return;
        }

        if (item.TmpText)
        {
            item.TmpText.text = content;
        }

        if (item.LegacyText)
        {
            item.LegacyText.text = content;
        }
    }

    /// <summary>
    /// 获取文本在当前样式下的首选尺寸。
    /// 这个尺寸用于设置 RectTransform.sizeDelta，避免文本显示区域过小。
    /// </summary>
    private Vector2 GetPreferredValues(BarrageTextItem item)
    {
        if (item == null)
        {
            return Vector2.zero;
        }

        if (item.TmpText)
        {
            item.TmpText.ForceMeshUpdate();
            return item.TmpText.GetPreferredValues(item.TmpText.text);
        }

        if (item.LegacyText)
        {
            return new Vector2(item.LegacyText.preferredWidth, item.LegacyText.preferredHeight);
        }

        return Vector2.zero;
    }

    /// <summary>
    /// 按当前配置应用文本样式。
    /// TMP 和原生 Text 的 API 不一致，因此分开处理。
    /// </summary>
    private void ApplyTextStyle(BarrageTextItem item)
    {
        if (item == null)
        {
            return;
        }

        if (item.TmpText)
        {
            if (!barrageFontAsset && !string.IsNullOrWhiteSpace(defaultFontResourcePath))
            {
                barrageFontAsset = Resources.Load<TMP_FontAsset>(defaultFontResourcePath);
            }

            if (barrageFontAsset)
            {
                item.TmpText.font = barrageFontAsset;
            }

            item.TmpText.raycastTarget = false;
            item.TmpText.alignment = TextAlignmentOptions.MidlineLeft;
            item.TmpText.fontSize = fontSize;
            item.TmpText.color = textColor;
            item.TmpText.outlineColor = outlineColor;
            item.TmpText.outlineWidth = outlineWidth;
            item.TmpText.enableWordWrapping = false;
            item.TmpText.overflowMode = TextOverflowModes.Overflow;
        }

        if (item.LegacyText)
        {
            EnsureLegacyTextFont(item.LegacyText);
            item.LegacyText.raycastTarget = false;
            item.LegacyText.alignment = TextAnchor.MiddleLeft;
            item.LegacyText.fontSize = Mathf.RoundToInt(fontSize);
            item.LegacyText.color = textColor;
            item.LegacyText.horizontalOverflow = HorizontalWrapMode.Overflow;
            item.LegacyText.verticalOverflow = VerticalWrapMode.Overflow;
            item.LegacyText.supportRichText = true;

            UnityEngine.UI.Outline outline = item.LegacyText.GetComponent<UnityEngine.UI.Outline>();
            if (outlineWidth > 0f)
            {
                if (!outline)
                {
                    outline = item.LegacyText.gameObject.AddComponent<UnityEngine.UI.Outline>();
                }

                outline.enabled = true;
                outline.effectColor = outlineColor;
                outline.effectDistance = Vector2.one * outlineWidth * 8f;
            }
            else if (outline)
            {
                outline.enabled = false;
            }
        }
    }

    /// <summary>
    /// 确保原生 Text 一定有字体。
    /// UnityEngine.UI.Text.font 为空时，运行时生成的弹幕可能完全不显示。
    /// </summary>
    private void EnsureLegacyTextFont(Text text)
    {
        if (!text)
        {
            return;
        }

        if (legacyTextFont)
        {
            text.font = legacyTextFont;
            return;
        }

        if (text.font)
        {
            legacyTextFont = text.font;
            return;
        }

        legacyTextFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (legacyTextFont)
        {
            text.font = legacyTextFont;
        }
    }

    /// <summary>
    /// 确保轨道时间数组和当前 laneCount 一致。
    /// </summary>
    private void EnsureLaneState()
    {
        int safeLaneCount = Mathf.Max(1, laneCount);
        if (nextLaneAvailableTimes != null && nextLaneAvailableTimes.Length == safeLaneCount)
        {
            return;
        }

        nextLaneAvailableTimes = new float[safeLaneCount];
    }

    /// <summary>
    /// 获取弹幕根节点宽度。布局尚未完成时用屏幕宽度兜底。
    /// </summary>
    private float GetRootWidth()
    {
        float width = barrageRoot.rect.width;
        return width > 1f ? width : Screen.width;
    }

    /// <summary>
    /// 获取弹幕根节点高度。布局尚未完成时用屏幕高度兜底。
    /// </summary>
    private float GetRootHeight()
    {
        float height = barrageRoot.rect.height;
        return height > 1f ? height : Screen.height;
    }
}
