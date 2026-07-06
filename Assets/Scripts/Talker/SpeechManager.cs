using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;

/***
 * This file will be updated in future.
 * BubbleText need to be modified to a prefab.
 * Use SayFromStr to speak a sentence from string param.
 * Function SpeakText & OnlySpeakText will be set private in future versions.
 */
[RequireComponent(typeof(AudioSource))]
public class SpeechManager : BaseController
{
    // 兼容旧调用方保留的模式枚举；实际合成器由 SpeechSynthesizerController 管理。
    public enum SpeechSynthesisMode
    {
        Azure,
        MimoTTS,
        CosyVoice
    }

    public enum SpeechVoiceGender
    {
        Male,
        Female
    }

    public static SpeechSynthesisMode SynthesisMode { get; private set; } = SpeechSynthesisMode.Azure;
    public static SpeechVoiceGender VoiceGender { get; private set; } = SpeechVoiceGender.Male;

    // public VirtualManController controller;
    private static readonly bool ShouldLoop = true;
    private static readonly int MaxLength = 10;
    private static readonly int Frequency = 44100;
    string _microphone = null;
    private AudioClip recordingClip = null;
    private AudioSource _audioSource;
    internal static SpeechManager Instance = null;
    private SpeechSynthesizer synthesizer;
    private bool isShuttingDown;
    private int testCnt = 0;

    //Is virHuman speaking
    private static bool isSpeaking;
    public static bool IsSpeaking => isSpeaking;
    public Speech2BlendshapeController speech2BlendshapeController;

    private bool IsRecognizing;

    // 供 SpeechSynthesizerController 判断宿主生命周期，避免卸载时继续回调 Unity 对象。
    public bool IsSpeechShuttingDown => isShuttingDown;
    // 非 Azure 合成器生成本地 AudioClip 后复用 SpeechManager 上的 AudioSource 播放。
    public AudioSource SpeechAudioSource => _audioSource;

    public static void SetSynthesisMode(SpeechSynthesisMode mode)
    {
        SynthesisMode = mode;
        var controller = ControllerRefer.Get<SpeechSynthesizerController>();
        controller?.SetSynthesisMode(ToControllerMode(mode));
        Debug.Log($"[SpeechManager] Speech synthesis mode: {SynthesisMode}");
    }

    public static void SetVoiceGender(SpeechVoiceGender gender)
    {
        VoiceGender = gender;
        var controller = ControllerRefer.Get<SpeechSynthesizerController>();
        controller?.SetVoiceGender(ToControllerGender(gender));
        Debug.Log($"[SpeechManager] Speech voice gender: {VoiceGender}");
    }

    public static SpeechSynthesisMode GetSynthesisMode()
    {
        // 从新 Controller 回读状态，保证旧的 SpeechManager.SynthesisMode 查询仍然可用。
        var controller = ControllerRefer.Get<SpeechSynthesizerController>();
        if (controller == null)
        {
            return SynthesisMode;
        }

        SynthesisMode = FromControllerMode(controller.SynthesisMode);
        return SynthesisMode;
    }

    private static SpeechSynthesizerController.SpeechSynthesisMode ToControllerMode(SpeechSynthesisMode mode)
    {
        return mode switch
        {
            SpeechSynthesisMode.MimoTTS => SpeechSynthesizerController.SpeechSynthesisMode.MimoTTS,
            SpeechSynthesisMode.CosyVoice => SpeechSynthesizerController.SpeechSynthesisMode.CosyVoice,
            _ => SpeechSynthesizerController.SpeechSynthesisMode.Azure
        };
    }

    private static SpeechSynthesisMode FromControllerMode(SpeechSynthesizerController.SpeechSynthesisMode mode)
    {
        return mode switch
        {
            SpeechSynthesizerController.SpeechSynthesisMode.MimoTTS => SpeechSynthesisMode.MimoTTS,
            SpeechSynthesizerController.SpeechSynthesisMode.CosyVoice => SpeechSynthesisMode.CosyVoice,
            _ => SpeechSynthesisMode.Azure
        };
    }

    private static SpeechSynthesizerController.SpeechVoiceGender ToControllerGender(SpeechVoiceGender gender)
    {
        return gender == SpeechVoiceGender.Female
            ? SpeechSynthesizerController.SpeechVoiceGender.Female
            : SpeechSynthesizerController.SpeechVoiceGender.Male;
    }

    private static string GetAzureVoiceNameByGender(SpeechVoiceGender gender)
    {
        return gender == SpeechVoiceGender.Female ? AzureAuth.FemaleVoiceName : AzureAuth.MaleVoiceName;
    }

    private static SpeechVoiceGender GetGenderByAzureVoiceName(string voiceName)
    {
        return voiceName == AzureAuth.FemaleVoiceName ? SpeechVoiceGender.Female : SpeechVoiceGender.Male;
    }

    public override void OnRegister()
    {
        base.OnRegister();
        Init();
    }

    public override void OnUnregister()
    {
        ShutdownSpeechSynthesizer();
        base.OnUnregister();
    }

    void Init()
    {
        isShuttingDown = false;
        Instance = this;
        // 提前创建统一合成 Controller；旧 Azure 初始化逻辑作为兜底保留在本类中。
        ControllerRefer.Get<SpeechSynthesizerController>();
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        if (Microphone.devices.IsEmpty())
        {
            TextBubble.SetGlobalText("未找到麦克风");
            Debug.LogWarning("[SR] No input devices found.", gameObject);
        }
        else
        {
            var microphone = Microphone.devices[0];
            Debug.Log($"[SR] Using device: {microphone}.", gameObject);
            _microphone = microphone;
        }
    }

    private void InitSpeechSynthesizer()
    {
        if (synthesizer != null)
        {
            return;
        }

        synthesizer = new SpeechSynthesizer(AzureAuth.SpeechConfig);
        synthesizer.SynthesisStarted += (self, args) =>
        {
            Debug.Log("text SynthesisStarted: Synthesis completed: {args.Result.Reason}");
            isSpeaking = true;
            // Azure 的 AudioOffset 是相对本次语音开始的时间，需要在语音开始时建立本地时间基准。
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                if (!isShuttingDown && speech2BlendshapeController != null)
                {
                    speech2BlendshapeController.BeginVisemeSchedule();
                }
            });
        };
        synthesizer.SynthesisCompleted += (self, args) =>
        {
            Debug.Log("text SynthesisCompleted: Synthesis completed: " + args.Result.Reason);
            isSpeaking = false;
            // 语音结束后清空尚未触发的口型，避免残留口型影响下一句。
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                if (speech2BlendshapeController != null)
                {
                    speech2BlendshapeController.EndVisemeSchedule();
                }
            });
        };
        synthesizer.SynthesisCanceled += (self, args) =>
        {
            Debug.Log("text SynthesisCanceled: Synthesis completed: " + args.Result.Reason);
            isSpeaking = false;
            // 语音被打断时也要清空口型队列，避免旧 viseme 延迟触发。
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                if (speech2BlendshapeController != null)
                {
                    speech2BlendshapeController.EndVisemeSchedule();
                }
            });
        };

        synthesizer.VisemeReceived += (sender, e) =>
        {

            Debug.Log($"[Viseme] ID: {e.VisemeId}, Time: {e.AudioOffset / 10000} ms, Animation Length: {e.Animation.Length}");
            string animationJson = e.Animation;

            // 必须回到主线程访问 Unity 对象；具体口型由 AudioOffset 决定触发时机。
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                if (!isShuttingDown && speech2BlendshapeController != null)
                {
                    speech2BlendshapeController.ScheduleViseme(e.VisemeId, e.AudioOffset / 10000000f);
                }
            });
        };
    }

    public void SetSynthesisVoice(string voiceName)
    {
        if (string.IsNullOrEmpty(voiceName))
        {
            return;
        }

        // introduce UI 仍传 Azure voice name；这里把它转换为统一的男女声信息。
        VoiceGender = GetGenderByAzureVoiceName(voiceName);
        var controller = ControllerRefer.Get<SpeechSynthesizerController>();
        controller?.SetVoiceByAzureVoiceName(voiceName);
        if (voiceName == AzureAuth.SpeechSynthesisVoiceName)
        {
            return;
        }

        AzureAuth.SetSpeechSynthesisVoiceName(voiceName);
    }

    private void ShutdownSpeechSynthesizer()
    {
        if (isShuttingDown)
        {
            return;
        }

        isShuttingDown = true;
        isSpeaking = false;

        if (Instance == this)
        {
            Instance = null;
        }

        StopLocalSpeechPlayback();
        ControllerRefer.Get<SpeechSynthesizerController>()?.ShutdownImmediately();

        if (synthesizer == null)
        {
            return;
        }

        try
        {
            synthesizer.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SpeechManager] Dispose synthesizer failed during shutdown: {e.Message}");
        }
        finally
        {
            synthesizer = null;
        }
    }

    private void StopLocalSpeechPlayback()
    {
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }

        if (speech2BlendshapeController != null)
        {
            speech2BlendshapeController.EndVisemeSchedule();
        }

        TextBubble.SetGlobalText(string.Empty);
    }

    AudioClip StartRecording()
    {
        return Microphone.Start(_microphone, ShouldLoop, MaxLength, Frequency);
    }

    AudioInputStream OpenClipStream(AudioClip clip)
    {
        var stream = AudioInputStream.CreatePushStream();
        stream.Write(clip.ToByteArray());
        return stream;
    }

    async Task<SpeechRecognitionResult> RecognizeClip(AudioClip clip)
    {
        using var stream = OpenClipStream(clip);
        using var audioConfig = AudioConfig.FromStreamInput(stream);
        var speechConfig = AzureAuth.SpeechConfig;
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
        return await recognizer.RecognizeOnceAsync();
    }

    void StopRecording() => Microphone.End(_microphone);

    public void _StartRecognizing()
    {
        recordingClip ??= StartRecording();
        Debug.Log($"[SR]: Speech started.");
    }

    public void _StopRecognizing()
    {
        StopRecording();
        var clip = recordingClip;
        recordingClip = null;
        Debug.Log("[SR]: Speech ended.");

        // this.RunTask(RecognizeAndProcess(clip));
    }

    public void StartRecognizing()
    {
        // TODO: Manage the task.
        _ = RecognizeAndProcess();
        // this.RunTask(RecognizeAndProcess());
    }

    public void StopRecognizing()
    {
        // this.RunTask(speechRecognizer.StopContinuousRecognitionAsync());
    }

    async Task RecognizeAndProcess()
    {
        using var speechRecognizer = new SpeechRecognizer(AzureAuth.SpeechConfig);
        var text = await speechRecognizer.ContinuousRecognizeString();

        Debug.Log($"[SR]: Spoken: {text}");
        // Configure await is true, so main thread invocation is not necessary.
        ProcessRecognizedText(text);
        // MainThreadDispatcher.InvokeOnMainThread(() =>
        // {
        //     ProcessRecognizedText(text);
        // });
    }

    //todo 此函数内的文本由乱码转化而来，需要确认
    void ProcessRecognizedText(string text)
    {
        if (text.Contains("向后转"))
        {
            //controlled.GetComponent<IHumanControl>().TurnBack();
            this.RunTask(SpeakText("好的"));
        }
    }

    public static void SayFromStr(string str, Action onSpeakComplete = null)
    {
        Debug.Log($"Msg in SayFromStr: {str}");
        if (Instance != null && !Instance.isShuttingDown)
        {
            var speakTask = Instance.OnlySpeakText(str, onSpeakComplete);
            Instance.RunTask(speakTask);
            isSpeaking = true;
        }
        else
        {
            onSpeakComplete?.Invoke();
        }
    }

    public static void ForceStop()
    {
        if (Instance != null)
        {
            Instance.RunTask(Instance.ForceStopSpeak());
        }
    }

    public void TestSayFromStr()
    {
        SayFromStr("测试语音");
    }

    public async Task SpeakText(string text)
    {
        using var synthesis = new SpeechSynthesizer(AzureAuth.SpeechConfig);
        synthesis.Synthesizing += (self, args) =>
        {
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                if (!isShuttingDown)
                {
                    // controller.LongTalk();
                    TextBubble.SetGlobalText(text);
                }
            });
        };
        synthesis.SynthesisCompleted += (self, args) =>
        {
            Debug.Log($"[{nameof(SpeakText)}]: Synthesis completed: {args.Result.Reason}", gameObject);
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                if (!isShuttingDown)
                {
                    TextBubble.SetGlobalText(string.Empty);
                    // controller.StopTalk();
                }
            });
        };
        var result = await synthesis.SpeakSsmlAsync(AzureSpeechSynthesizerBackend.BuildAzureSsml(CleanSpeechText(text), true)).ConfigureAwait(false);
        //var result = await synthesis.SpeakTextAsync(text).ConfigureAwait(false);
        MainThreadDispatcher.InvokeOnMainThread(() =>
        {
            if (!isShuttingDown)
            {
                TextBubble.SetGlobalText(string.Empty);
                // controller.StopTalk();
            }
        });
        Debug.Log($"Msg: {result.AudioData.Length}");
        // var clip = MakeClip(result.AudioData);
        // _audioSource.clip = clip;
        // _audioSource.Play();
    }

    public async Task OnlySpeakText(string text, Action onSpeakComplete = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("Msg: Empty String");
            return;
        }

        // 新路径：SpeechManager 只负责对外入口、字幕和音频宿主，具体 TTS provider 交给 Controller。
        var speechSynthesizerController = ControllerRefer.Get<SpeechSynthesizerController>();
        if (speechSynthesizerController != null)
        {
            await speechSynthesizerController.SpeakText(this, text, onSpeakComplete).ConfigureAwait(false);
            return;
        }

        if (isShuttingDown || synthesizer == null)
        {
            return;
        }

        try
        {
            await synthesizer.StopSpeakingAsync().ConfigureAwait(false);
            if (isShuttingDown || synthesizer == null)
            {
                return;
            }

            Debug.Log("Msg: " + text + "prepare");
            var result = await synthesizer.SpeakSsmlAsync(AzureSpeechSynthesizerBackend.BuildAzureSsml(CleanSpeechText(text), true)).ConfigureAwait(false);
            if (isShuttingDown)
            {
                return;
            }

            Debug.Log("Msg: " + text + "result" + result.AudioData.Length);
            if (onSpeakComplete != null)
            {
                MainThreadDispatcher.InvokeOnMainThread(onSpeakComplete);
            }
        }
        catch (ObjectDisposedException)
        {
            // Unity domain reload or scene unload disposed the synthesizer while speech was running.
        }
        catch (Exception e)
        {
            if (!isShuttingDown)
            {
                Debug.LogError($"[SpeechManager] Speak text failed: {e}");
            }
        }
    }

    public static string CleanSpeechText(string text)
    {
        return text.Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "");
    }

    // 外部合成器开始播放前统一设置字幕、说话状态和口型调度基准。
    public void BeginExternalSpeech(string text)
    {
        isSpeaking = true;
        if (!string.IsNullOrEmpty(text))
        {
            TextBubble.SetGlobalText(text);
        }
        BeginLocalSpeechSchedule();
    }

    // Azure 自带播放结束事件会走这里，只结束口型和说话状态，不触发业务完成回调。
    public void EndExternalSpeechSchedule()
    {
        isSpeaking = false;
        if (speech2BlendshapeController != null)
        {
            speech2BlendshapeController.EndVisemeSchedule();
        }
    }

    // 非 Azure 合成器完整播放结束或被中断时走这里，统一清理字幕、音频和口型状态。
    public void FinishExternalSpeech(Action onSpeakComplete)
    {
        isSpeaking = false;
        TextBubble.SetGlobalText(string.Empty);
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
        if (speech2BlendshapeController != null)
        {
            speech2BlendshapeController.EndVisemeSchedule();
        }
        onSpeakComplete?.Invoke();
    }

    private void BeginLocalSpeechSchedule()
    {
        if (speech2BlendshapeController != null)
        {
            speech2BlendshapeController.BeginVisemeSchedule();
        }
    }

    public async Task ForceStopSpeak()
    {
        var speechSynthesizerController = ControllerRefer.Get<SpeechSynthesizerController>();
        if (speechSynthesizerController != null)
        {
            await speechSynthesizerController.StopSpeaking(this).ConfigureAwait(false);
        }

        if (synthesizer != null)
        {
            _ = synthesizer.StopSpeakingAsync();
        }
        isSpeaking = false;
        MainThreadDispatcher.InvokeOnMainThread(() =>
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
            if (speech2BlendshapeController != null)
            {
                speech2BlendshapeController.EndVisemeSchedule();
            }
            TextBubble.SetGlobalText(string.Empty);
        });
    }

    private AudioClip MakeClip(byte[] data)
    {
        var floatData = data.ToFloatArray();
        var clip = AudioClip.Create("testSound", floatData.Length, 1, 44100, false);
        clip.SetData(floatData, 0);
        return clip;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDestroy()
    {
        ShutdownSpeechSynthesizer();
    }

    private void OnDisable()
    {
        ShutdownSpeechSynthesizer();
    }

    private void OnApplicationQuit()
    {
        ShutdownSpeechSynthesizer();
    }
}
