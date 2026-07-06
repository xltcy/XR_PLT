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
    [SerializeField] private TextMeshProUGUI itemTemplate;

    [Header("布局")]
    [SerializeField] private int laneCount = 8;
    [SerializeField] private float laneHeight = 48f;
    [SerializeField] private float paddingTop = 32f;
    [SerializeField] private float minHorizontalGap = 80f;
    [SerializeField] private int maxPendingMessages = 200;
    [SerializeField] private int maxActiveItems = 40;

    [Header("动画")]
    [SerializeField] private float moveSpeed = 420f;
    [SerializeField] private float spawnInterval = 0.12f;
    [SerializeField] private TMP_FontAsset barrageFontAsset;
    [SerializeField] private string defaultFontResourcePath = "Fonts/simhei SDF";
    [SerializeField] private float fontSize = 30f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private float outlineWidth = 0.18f;

    private readonly Queue<InteractiveBarrageMessage> pendingMessages = new Queue<InteractiveBarrageMessage>();
    private readonly Queue<TextMeshProUGUI> itemPool = new Queue<TextMeshProUGUI>();
    private readonly List<ActiveBarrageItem> activeItems = new List<ActiveBarrageItem>();
    private float[] nextLaneAvailableTimes;
    private float nextSpawnTime;

    private class ActiveBarrageItem
    {
        public TextMeshProUGUI Text;
        public RectTransform RectTransform;
        public float EndX;
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
        TextMeshProUGUI text = GetItem();
        RectTransform rectTransform = text.rectTransform;

        text.text = FormatMessage(message);
        ApplyTextStyle(text);
        text.ForceMeshUpdate();

        Vector2 preferredValues = text.GetPreferredValues(text.text);
        float width = Mathf.Max(120f, preferredValues.x + 48f);
        float height = Mathf.Max(laneHeight, preferredValues.y + 12f);
        rectTransform.sizeDelta = new Vector2(width, height);

        float rootWidth = GetRootWidth();
        float rootHeight = GetRootHeight();
        float startX = rootWidth * 0.5f + width * 0.5f;
        float endX = -rootWidth * 0.5f - width * 0.5f;
        float y = rootHeight * 0.5f - paddingTop - laneHeight * 0.5f - lane * laneHeight;

        rectTransform.anchoredPosition = new Vector2(startX, y);
        text.gameObject.SetActive(true);

        activeItems.Add(new ActiveBarrageItem
        {
            Text = text,
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

    private TextMeshProUGUI GetItem()
    {
        if (itemPool.Count > 0)
        {
            return itemPool.Dequeue();
        }

        TextMeshProUGUI text = Instantiate(itemTemplate, barrageRoot);
        text.name = "BarrageItem";
        return text;
    }

    private void RecycleItem(ActiveBarrageItem item)
    {
        if (item == null || !item.Text)
        {
            return;
        }

        item.Text.gameObject.SetActive(false);
        itemPool.Enqueue(item.Text);
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
            return;
        }

        GameObject templateObject = new GameObject("BarrageItemTemplate");
        templateObject.transform.SetParent(barrageRoot, false);
        itemTemplate = templateObject.AddComponent<TextMeshProUGUI>();
        itemTemplate.raycastTarget = false;
        ApplyTextStyle(itemTemplate);

        RectTransform rectTransform = itemTemplate.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(400f, laneHeight);

        itemTemplate.gameObject.SetActive(false);
    }

    private void ApplyTextStyle(TextMeshProUGUI text)
    {
        if (!text)
        {
            return;
        }

        if (!barrageFontAsset && !string.IsNullOrWhiteSpace(defaultFontResourcePath))
        {
            barrageFontAsset = Resources.Load<TMP_FontAsset>(defaultFontResourcePath);
        }

        if (barrageFontAsset)
        {
            text.font = barrageFontAsset;
        }

        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.fontSize = fontSize;
        text.color = textColor;
        text.outlineColor = outlineColor;
        text.outlineWidth = outlineWidth;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
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
