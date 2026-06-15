using System.Collections.Generic;
using System.Text;
using TickSystem;
using UnityEngine;

/// <summary>
/// 驱动当前讲解员头部模型的口型 BlendShape。
///
/// 当前支持两种口型输入模式：
/// - Azure viseme 模式：Azure 会返回带时间偏移的 visemeId，这是更精确的模式，可以使用下面完整的
///   visemeId 到 BlendShape 的映射表。
/// - 音频驱动模式：CosyVoice 等服务只返回 PCM 音频，不返回 viseme 时间戳。该模式会分析 Unity
///   实际播放出去的 PCM 帧，并粗略选择一个接近的口型。
/// </summary>
public class Speech2BlendshapeController : MonoBehaviour, ITickerUpdate
{
    public GameObject guideHead;

    // 口型张开的插值速度。数值越大，BlendShape 越快接近目标张开值。
    [SerializeField] private float openSpeed = 14f;

    // 口型闭合的插值速度。数值越大，嘴部越快回到自然闭合状态。
    [SerializeField] private float closeSpeed = 8f;

    // 默认口型强度百分比，后续会按当前模型的 BlendShape 最大帧权重进行缩放。
    [SerializeField] private float blendWeight = 50f;

    // PCM 驱动口型的最小 RMS 阈值。低于该值的音频帧会被当作静音处理。
    [SerializeField] private float audioLipMinRms = 0.012f;

    // PCM 驱动口型的最大 RMS 参考值。达到或超过该值时会使用最强的嘴部运动幅度。
    [SerializeField] private float audioLipMaxRms = 0.12f;

    // PCM 分析窗口长度。20ms 能兼顾口型响应速度和逐帧抖动控制。
    [SerializeField] private int audioLipFrameMs = 20;

    private SkinnedMeshRenderer smr;

    // 归一化后的 BlendShape 名称或别名 -> BlendShape 索引。
    private readonly Dictionary<string, int> blendShapeIndexCache = new Dictionary<string, int>();

    // 缺失的 BlendShape 别名只提示一次，避免长语音过程中重复刷日志。
    private readonly HashSet<string> warnedMissingBlendShapes = new HashSet<string>();
    // Azure viseme 回调可能提前到达，先缓存到队列里，Tick 到对应音频时间再执行。
    private readonly Queue<ScheduledViseme> scheduledVisemes = new Queue<ScheduledViseme>();
    private readonly Queue<float> audioDrivenSamples = new Queue<float>();
    private readonly object audioDrivenLock = new object();
    private int activeBlendShapeIndex = -1;
    private float activeTargetWeight;
    private float speechStartTime;
    private bool hasSpeechStartTime;
    private bool audioDrivenLipSyncActive;
    private int audioDrivenSampleRate = 24000;
    private float lastAudioDrivenSampleTime;
    private float nextAudioDrivenFrameTime;

    private struct ScheduledViseme
    {
        public uint VisemeId;
        public float TargetTime;
    }

    // Azure 返回的是数字 visemeId，不是具体模型上的 BlendShape 名称。这里为每个 visemeId 配置一组
    // 按优先级排列的 BlendShape 别名，运行时会使用模型上第一个能匹配到的名称。
    //
    // 这张表不是只使用 mouthOpen：
    // - 元音类口型：aa、oh、E、ih。
    // - 圆唇/收口口型：mouthFunnel。
    // - 辅音类口型：SS、CH、TH、FF、DD、kk、PP、RR。
    // - 通用张嘴 fallback：mouthOpen、jawOpen。
    //
    // 如果模型只提供 mouthOpen，那么只有包含该 fallback 的 visemeId 能驱动它。要获得更好的口型效果，
    // 模型应尽量提供与这些别名匹配的 viseme 或 ARKit 风格 BlendShape。
    private Dictionary<uint, string[]> visemeToBlendShape = new Dictionary<uint, string[]>
    {
        { 0u, null },
        { 1u, Names("aa", "viseme_aa") },
        { 2u, Names("aa", "viseme_aa") },
        { 3u, Names("oh", "viseme_oh", "O") },
        { 4u, Names("E", "viseme_E", "ee") },
        { 5u, Names("RR", "viseme_RR", "R") },
        { 6u, Names("ih", "viseme_ih") },
        { 7u, Names("oh", "viseme_oh", "O") },
        { 8u, Names("oh", "viseme_oh", "O") },
        { 9u, Names("oh", "viseme_oh", "O") },
        { 10u, Names("oh", "viseme_oh", "O") },
        { 11u, Names("aa", "viseme_aa") },
        { 12u, Names("mouthOpen", "viseme_mouthOpen", "jawOpen", "aa") },
        { 13u, Names("RR", "viseme_RR", "R") },
        { 14u, Names("mouthFunnel", "viseme_mouthFunnel", "funnel", "oh") },
        { 15u, Names("SS", "viseme_SS", "S") },
        { 16u, Names("CH", "viseme_CH") },
        { 17u, Names("TH", "viseme_TH") },
        { 18u, Names("FF", "viseme_FF", "F") },
        { 19u, Names("DD", "viseme_DD", "D") },
        { 20u, Names("kk", "viseme_kk", "K") },
        { 21u, Names("PP", "viseme_PP", "P") },
    };

    #region Unity生命周期

    private void Start()
    {
        SetGuideHead(guideHead);
    }

    private void OnEnable()
    {
        TickController.RegisterTick(this);
    }

    private void OnDisable()
    {
        TickController.UnRegisterTick(this);
    }

    public void Tick()
    {
        if (!smr || !smr.sharedMesh)
        {
            return;
        }

        // 每一帧只允许一种来源驱动口型。Azure viseme 和 PCM 分析不能同时写 activeBlendShapeIndex，
        // 否则两个来源会互相覆盖当前口型。
        if (audioDrivenLipSyncActive)
        {
            ProcessAudioDrivenLipSync();
        }
        else
        {
            ProcessScheduledVisemes();
        }

        for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
        {
            float targetWeight = i == activeBlendShapeIndex ? activeTargetWeight : 0f;
            // 口型时序已经决定了当前应该激活哪个 BlendShape；这里的逐帧插值只负责视觉过渡，
            // 避免口型在不同姿态之间硬切。
            // AudioOffset 决定口型节奏；这里的插值只负责视觉过渡，避免 BlendShape 硬切。
            float speed = targetWeight > smr.GetBlendShapeWeight(i) ? openSpeed : closeSpeed;
            float weight = Mathf.Lerp(smr.GetBlendShapeWeight(i), targetWeight, 1f - Mathf.Exp(-speed * Time.deltaTime));
            if (weight < 0.001f)
            {
                weight = 0f;
            }

            smr.SetBlendShapeWeight(i, weight);
        }
    }

    #endregion

    #region 公开接口

    /// <summary>
    /// 返回某个 Azure visemeId 的首选 BlendShape 别名。
    /// 实际运行时仍会按 visemeToBlendShape 中配置的所有别名依次尝试匹配。
    /// </summary>
    public string GetBlendshapeName(uint i)
    {
        if (!visemeToBlendShape.TryGetValue(i, out string[] names) || names == null || names.Length == 0)
        {
            return null;
        }

        return names[0];
    }

    /// <summary>
    /// 开始 Azure viseme 调度，并清理 PCM 驱动口型的状态。
    /// Azure viseme 数据包含语音时间偏移，因此比纯音频分析更精确。
    /// </summary>
    public void BeginVisemeSchedule()
    {
        audioDrivenLipSyncActive = false;
        ClearAudioDrivenSamples();
        scheduledVisemes.Clear();
        // Time.realtimeSinceStartup 不受 timeScale 影响，更适合作为语音播放时间轴。
        speechStartTime = Time.realtimeSinceStartup;
        hasSpeechStartTime = true;
        ClearVisemeTarget();
    }

    /// <summary>
    /// 停止当前所有口型输入，并让当前口型目标回到自然状态。
    /// 语音结束、语音被打断或停止 Play Mode 时都会调用它。
    /// </summary>
    public void EndVisemeSchedule()
    {
        scheduledVisemes.Clear();
        audioDrivenLipSyncActive = false;
        ClearAudioDrivenSamples();
        hasSpeechStartTime = false;
        ClearVisemeTarget();
    }

    /// <summary>
    /// 开始 PCM/音频驱动口型，用于不提供 viseme 时间戳的 TTS 后端。
    /// </summary>
    /// <param name="sampleRate">PCM 采样率。传入非法值时会回退到 24000Hz。</param>
    public void BeginAudioDrivenLipSync(int sampleRate)
    {
        scheduledVisemes.Clear();
        hasSpeechStartTime = false;
        audioDrivenSampleRate = sampleRate > 0 ? sampleRate : 24000;
        audioDrivenLipSyncActive = true;
        lastAudioDrivenSampleTime = Time.realtimeSinceStartup;
        nextAudioDrivenFrameTime = lastAudioDrivenSampleTime;
        ClearAudioDrivenSamples();
        ClearVisemeTarget();
    }

    /// <summary>
    /// 将小端有符号 PCM16 数据加入队列，用于近似口型分析。
    /// 当某个后端直接拿到原始 PCM 字节流时使用这个入口。
    /// </summary>
    /// <param name="pcmData">小端字节序的单声道 PCM16 采样数据。</param>
    /// <param name="sampleRate">PCM 数据采样率。</param>
    public void PushPcm16ForLipSync(byte[] pcmData, int sampleRate)
    {
        if (pcmData == null || pcmData.Length < 2)
        {
            return;
        }

        if (!audioDrivenLipSyncActive)
        {
            BeginAudioDrivenLipSync(sampleRate);
        }

        audioDrivenSampleRate = sampleRate > 0 ? sampleRate : audioDrivenSampleRate;
        int maxBufferedSamples = Mathf.Max(audioDrivenSampleRate, audioDrivenSampleRate * 2);
        lock (audioDrivenLock)
        {
            int evenLength = pcmData.Length - pcmData.Length % 2;
            for (int i = 0; i < evenLength; i += 2)
            {
                short value = (short)(pcmData[i] | (pcmData[i + 1] << 8));
                audioDrivenSamples.Enqueue(value / 32768f);
            }

            while (audioDrivenSamples.Count > maxBufferedSamples)
            {
                audioDrivenSamples.Dequeue();
            }
        }

        lastAudioDrivenSampleTime = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// 将归一化后的 float PCM 采样加入队列，用于近似口型分析。
    /// CosyVoice 会在 AudioClip.OnAudioRead 中调用该方法，因此口型跟随的是 Unity 实际播放的音频，
    /// 而不是可能更快到达的网络流接收速度。
    /// </summary>
    /// <param name="samples">范围为 -1..1 的归一化 PCM 采样。</param>
    /// <param name="sampleCount">数组开头有效采样数量。</param>
    /// <param name="sampleRate">PCM 数据采样率。</param>
    public void PushPcmSamplesForLipSync(float[] samples, int sampleCount, int sampleRate)
    {
        if (samples == null || sampleCount <= 0)
        {
            return;
        }

        audioDrivenSampleRate = sampleRate > 0 ? sampleRate : audioDrivenSampleRate;
        int maxBufferedSamples = System.Math.Max(audioDrivenSampleRate, audioDrivenSampleRate * 2);
        int count = System.Math.Min(sampleCount, samples.Length);
        lock (audioDrivenLock)
        {
            for (int i = 0; i < count; i++)
            {
                audioDrivenSamples.Enqueue(samples[i]);
            }

            while (audioDrivenSamples.Count > maxBufferedSamples)
            {
                audioDrivenSamples.Dequeue();
            }
        }
    }

    /// <summary>
    /// 向本地调度队列添加一个 Azure viseme 事件。
    /// </summary>
    /// <param name="visemeId">Azure visemeId，通常范围为 0..21。</param>
    /// <param name="audioOffsetSeconds">相对合成音频起点的时间偏移，单位为秒。</param>
    public void ScheduleViseme(uint visemeId, float audioOffsetSeconds)
    {
        if (!hasSpeechStartTime)
        {
            BeginVisemeSchedule();
        }

        // audioOffsetSeconds 是 Azure 返回的音频时间偏移，换算成本地绝对触发时间。
        scheduledVisemes.Enqueue(new ScheduledViseme
        {
            VisemeId = visemeId,
            TargetTime = speechStartTime + audioOffsetSeconds,
        });
    }

    /// <summary>
    /// 应用一个 Azure visemeId：先解析到当前模型上可用的第一个 BlendShape 别名，再设置为当前目标口型。
    /// </summary>
    private void ApplyViseme(uint visemeId)
    {
        if (!smr || !smr.sharedMesh)
        {
            return;
        }

        if (!visemeToBlendShape.TryGetValue(visemeId, out string[] blendShapeNames) || blendShapeNames == null)
        {
            ClearVisemeTarget();
            return;
        }

        int index = GetBlendShapeIndex(blendShapeNames);
        if (index < 0)
        {
            string warningKey = blendShapeNames.Length > 0 ? blendShapeNames[0] : visemeId.ToString();
            if (warnedMissingBlendShapes.Add(warningKey))
            {
                Debug.LogWarning($"BlendShape for visemeId {visemeId} was not found. Expected aliases: {string.Join(", ", blendShapeNames)}");
            }

            return;
        }

        SetVisemeTarget(index, blendWeight);
    }

    /// <summary>
    /// 立即重置当前网格上的所有 BlendShape，并清空等待中的口型状态。
    /// 切换模型或强制停止语音时使用。
    /// </summary>
    public void ResetAllBlendShapes()
    {
        if (!smr || !smr.sharedMesh)
        {
            return;
        }

        for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
        {
            smr.SetBlendShapeWeight(i, 0f);
        }

        scheduledVisemes.Clear();
        audioDrivenLipSyncActive = false;
        ClearAudioDrivenSamples();
        ClearVisemeTarget();
    }

    /// <summary>
    /// 绑定新的头部模型，并重建 BlendShape 别名缓存。
    /// 如果根节点上没有 SkinnedMeshRenderer，会继续在子节点中查找。
    /// </summary>
    public void SetGuideHead(GameObject head)
    {
        if (!head)
        {
            return;
        }

        guideHead = head;
        smr = guideHead.GetComponent<SkinnedMeshRenderer>();
        if (!smr)
        {
            smr = guideHead.GetComponentInChildren<SkinnedMeshRenderer>();
        }

        RebuildBlendShapeCache();
    }

    #endregion

    #region BlendShape索引缓存

    /// <summary>
    /// 从一组按优先级排列的别名中解析第一个可用的 BlendShape。
    /// 先尝试 Unity 网格中的精确名称匹配，再尝试归一化后的别名匹配。
    /// </summary>
    private int GetBlendShapeIndex(string[] candidateNames)
    {
        foreach (string candidateName in candidateNames)
        {
            if (string.IsNullOrEmpty(candidateName)) continue;

            int exactIndex = smr.sharedMesh.GetBlendShapeIndex(candidateName);
            if (exactIndex >= 0)
            {
                return exactIndex;
            }

            string normalizedName = NormalizeBlendShapeName(candidateName);
            if (blendShapeIndexCache.TryGetValue(normalizedName, out int cachedIndex))
            {
                return cachedIndex;
            }
        }

        return -1;
    }

    /// <summary>
    /// 为当前绑定的模型重建 BlendShape 别名查询表。
    /// 名称会按原始形式缓存；如果名称带有 viseme_ 前缀，也会额外缓存去掉前缀后的别名，
    /// 因此模型里的 viseme_aa 也可以被 aa 命中。
    /// </summary>
    private void RebuildBlendShapeCache()
    {
        blendShapeIndexCache.Clear();
        warnedMissingBlendShapes.Clear();

        if (!smr || !smr.sharedMesh)
        {
            return;
        }

        for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
        {
            string rawName = smr.sharedMesh.GetBlendShapeName(i);
            AddBlendShapeAlias(rawName, i);

            const string visemePrefix = "viseme_";
            if (rawName.StartsWith(visemePrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                AddBlendShapeAlias(rawName.Substring(visemePrefix.Length), i);
            }
        }
    }

    /// <summary>
    /// 向缓存中添加一个归一化后的 BlendShape 别名。
    /// 如果出现重复别名，保留第一次出现的索引，避免覆盖模型原始顺序。
    /// </summary>
    private void AddBlendShapeAlias(string name, int index)
    {
        string normalizedName = NormalizeBlendShapeName(name);
        if (!string.IsNullOrEmpty(normalizedName) && !blendShapeIndexCache.ContainsKey(normalizedName))
        {
            blendShapeIndexCache.Add(normalizedName, index);
        }
    }

    /// <summary>
    /// 将 0..100 的百分比转换成当前网格实际使用的 BlendShape 帧权重。
    /// 有些模型最大值是 100，也有模型会使用自定义的最终帧权重。
    /// </summary>
    private float GetScaledBlendShapeWeight(int index, float percent)
    {
        return Mathf.Clamp01(percent / 100f) * GetBlendShapeMaxWeight(index);
    }

    /// <summary>
    /// 设置当前激活的 BlendShape 目标。Tick 会平滑过渡到该目标，并让其他 BlendShape 回到 0。
    /// </summary>
    private void SetVisemeTarget(int index, float percent)
    {
        activeBlendShapeIndex = index;
        activeTargetWeight = GetScaledBlendShapeWeight(index, percent);
    }

    /// <summary>
    /// 根据别名列表设置当前激活的 BlendShape 目标。
    /// 这里会忽略缺失的别名，因为音频驱动模式可能只使用模型上存在的一部分口型。
    /// </summary>
    private void SetVisemeTargetByNames(string[] blendShapeNames, float percent)
    {
        int index = GetBlendShapeIndex(blendShapeNames);
        if (index < 0)
        {
            return;
        }

        SetVisemeTarget(index, percent);
    }

    /// <summary>
    /// 清空当前口型目标。之后 Tick 会把所有 BlendShape 插值回 0。
    /// </summary>
    private void ClearVisemeTarget()
    {
        activeBlendShapeIndex = -1;
        activeTargetWeight = 0f;
    }

    /// <summary>
    /// 处理已经到达触发时间的 Azure viseme 事件。
    /// 同一帧可能有多个事件到期，这里会全部消费，最终以最新的口型为准。
    /// </summary>
    private void ProcessScheduledVisemes()
    {
        float now = Time.realtimeSinceStartup;
        // 可能同一帧有多个已到点的 viseme，全部消费到最新口型。
        while (scheduledVisemes.Count > 0 && scheduledVisemes.Peek().TargetTime <= now)
        {
            ApplyViseme(scheduledVisemes.Dequeue().VisemeId);
        }
    }

    /// <summary>
    /// 根据 PCM 音频帧近似生成口型。
    ///
    /// 该方法并不知道真实音素，只能使用以下音频特征进行粗略判断：
    /// - RMS：整体响度，用来决定嘴巴张开幅度。
    /// - 过零率：噪声感或高频成分较强的帧，通常映射到 SS 类口型。
    /// - 平均绝对差分：波形变化更尖锐的帧，通常映射到 E 类口型。
    ///
    /// fallback 口型集合故意保持较小：SS、E、aa/mouthOpen 和 oh。这样可以让不提供 viseme 的 TTS
    /// 服务具备可用的口型表现，但不会假装它拥有 Azure viseme 级别的精确度。
    /// </summary>
    private void ProcessAudioDrivenLipSync()
    {
        float now = Time.realtimeSinceStartup;
        float frameDuration = Mathf.Max(10, audioLipFrameMs) / 1000f;
        if (now < nextAudioDrivenFrameTime)
        {
            return;
        }

        nextAudioDrivenFrameTime = now + frameDuration;
        int frameSize = Mathf.Max(1, audioDrivenSampleRate * Mathf.Max(10, audioLipFrameMs) / 1000);
        if (!TryDequeueAudioFrame(frameSize, out float[] frame))
        {
            if (GetAudioDrivenBufferedSampleCount() <= 0 && now - lastAudioDrivenSampleTime > 0.35f)
            {
                ClearVisemeTarget();
            }

            return;
        }

        lastAudioDrivenSampleTime = now;
        float sumSquares = 0f;
        float sumAbsDelta = 0f;
        int zeroCrossings = 0;
        float previous = frame[0];
        for (int i = 0; i < frame.Length; i++)
        {
            float sample = frame[i];
            sumSquares += sample * sample;
            if (i > 0)
            {
                sumAbsDelta += Mathf.Abs(sample - previous);
                if ((sample >= 0f && previous < 0f) || (sample < 0f && previous >= 0f))
                {
                    zeroCrossings++;
                }
            }

            previous = sample;
        }

        float rms = Mathf.Sqrt(sumSquares / frame.Length);
        if (rms < audioLipMinRms)
        {
            ClearVisemeTarget();
            return;
        }

        float level = Mathf.InverseLerp(audioLipMinRms, audioLipMaxRms, rms);
        float targetWeight = Mathf.Lerp(blendWeight * 0.35f, blendWeight, level);
        float zeroCrossingRate = zeroCrossings / (float)frame.Length;
        float edgeDensity = sumAbsDelta / frame.Length;

        if (zeroCrossingRate > 0.18f)
        {
            SetVisemeTargetByNames(Names("SS", "viseme_SS", "S"), targetWeight * 0.75f);
        }
        else if (edgeDensity > rms * 0.9f)
        {
            SetVisemeTargetByNames(Names("E", "viseme_E", "ee"), targetWeight * 0.85f);
        }
        else if (level > 0.65f)
        {
            SetVisemeTargetByNames(Names("aa", "viseme_aa", "mouthOpen", "jawOpen"), targetWeight);
        }
        else
        {
            SetVisemeTargetByNames(Names("oh", "viseme_oh", "O"), targetWeight * 0.8f);
        }
    }

    /// <summary>
    /// 从 PCM 队列中取出一个固定长度的分析帧。
    /// 返回 false 只表示当前已播放音频还不够一帧，不一定代表语音已经结束。
    /// </summary>
    private bool TryDequeueAudioFrame(int frameSize, out float[] frame)
    {
        frame = null;
        lock (audioDrivenLock)
        {
            if (audioDrivenSamples.Count < frameSize)
            {
                return false;
            }

            frame = new float[frameSize];
            for (int i = 0; i < frameSize; i++)
            {
                frame[i] = audioDrivenSamples.Dequeue();
            }

            return true;
        }
    }

    /// <summary>
    /// 清空音频驱动 fallback 路径中缓存的 PCM 采样。
    /// </summary>
    private void ClearAudioDrivenSamples()
    {
        lock (audioDrivenLock)
        {
            audioDrivenSamples.Clear();
        }
    }

    /// <summary>
    /// 返回等待音频驱动口型分析的已播放 PCM 采样数量。
    /// </summary>
    private int GetAudioDrivenBufferedSampleCount()
    {
        lock (audioDrivenLock)
        {
            return audioDrivenSamples.Count;
        }
    }

    /// <summary>
    /// 读取 BlendShape 的最终帧权重。Unity 模型常见最大值是 100，但这里兼容自定义权重范围。
    /// </summary>
    private float GetBlendShapeMaxWeight(int index)
    {
        Mesh mesh = smr.sharedMesh;
        int frameCount = mesh.GetBlendShapeFrameCount(index);
        if (frameCount <= 0)
        {
            return 100f;
        }

        return mesh.GetBlendShapeFrameWeight(index, frameCount - 1);
    }

    /// <summary>
    /// 对 BlendShape 名称做归一化，便于宽松匹配。
    /// 大小写、空格、下划线、连字符以及其他非字母数字字符都会被忽略。
    /// 例如 viseme_RR、Viseme RR 和 viseme-RR 都会归一化为 visemerr。
    /// </summary>
    private static string NormalizeBlendShapeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(name.Length);
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// 用于在映射表中简洁声明一组按优先级排列的 BlendShape 别名。
    /// </summary>
    private static string[] Names(params string[] names)
    {
        return names;
    }

    #endregion
}
