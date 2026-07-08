using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 弹幕显示组件。
/// 负责把收到的消息排队，按固定车道从右向左播放，并复用 TextMeshProUGUI 对象减少运行时 GC 和 Instantiate 开销。
/// </summary>
public class BarrageDisplay : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private RectTransform barrageRoot;
    [SerializeField] private Graphic itemTemplate;

    [Header("布局")]
    [SerializeField] private int laneCount = 8;
    [SerializeField] private float laneHeight = 48f;
    [SerializeField] private float laneVerticalGap = 8f;
    [SerializeField] private float paddingTop = 32f;
    [SerializeField] private float minHorizontalGap = 80f;
    [SerializeField] private int maxPendingMessages = 200;
    [SerializeField] private int maxActiveItems = 40;

    [Header("动画")]
    [SerializeField] private float moveSpeed = 420f;
    [SerializeField] private float spawnInterval = 0.12f;
    [SerializeField] private TMP_FontAsset barrageFontAsset;
    [SerializeField] private string defaultFontResourcePath = "Fonts/simhei SDF";
    [SerializeField] private Font legacyTextFont;
    [SerializeField] private float fontSize = 30f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private float outlineWidth = 0.18f;

    private readonly Queue<InteractiveBarrageMessage> pendingMessages = new Queue<InteractiveBarrageMessage>();
    private readonly Queue<BarrageTextItem> itemPool = new Queue<BarrageTextItem>();
    private readonly List<ActiveBarrageItem> activeItems = new List<ActiveBarrageItem>();
    private float[] nextLaneAvailableTimes;
    private float nextSpawnTime;

    private class ActiveBarrageItem
    {
        public BarrageTextItem TextItem;
        public RectTransform RectTransform;
        public float EndX;
    }

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
        EnsureLaneState();
        SpawnPendingMessages();
        MoveActiveItems();
    }

    /// <summary>
    /// 把一条弹幕加入显示队列。队列满时会丢弃最旧的未显示消息，避免高并发时拖垮 UI。
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

        Vector2 preferredValues = GetPreferredValues(textItem);
        float width = Mathf.Max(120f, preferredValues.x + 48f);
        float height = Mathf.Max(laneHeight, preferredValues.y + 12f);
        rectTransform.sizeDelta = new Vector2(width, height);

        float rootWidth = GetRootWidth();
        float rootHeight = GetRootHeight();
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

        float spacingTime = (width + minHorizontalGap) / Mathf.Max(1f, moveSpeed);
        nextLaneAvailableTimes[lane] = Time.time + spacingTime;
        nextSpawnTime = Time.time + spawnInterval;
    }

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

    private void RecycleItem(ActiveBarrageItem item)
    {
        if (item == null || item.TextItem == null || !item.TextItem.IsValid)
        {
            return;
        }

        item.TextItem.GameObject.SetActive(false);
        itemPool.Enqueue(item.TextItem);
    }

    private string FormatMessage(InteractiveBarrageMessage message)
    {
        return message.content;
    }

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

    private void EnsureLaneState()
    {
        int safeLaneCount = Mathf.Max(1, laneCount);
        if (nextLaneAvailableTimes != null && nextLaneAvailableTimes.Length == safeLaneCount)
        {
            return;
        }

        nextLaneAvailableTimes = new float[safeLaneCount];
    }

    private float GetRootWidth()
    {
        float width = barrageRoot.rect.width;
        return width > 1f ? width : Screen.width;
    }

    private float GetRootHeight()
    {
        float height = barrageRoot.rect.height;
        return height > 1f ? height : Screen.height;
    }
}
