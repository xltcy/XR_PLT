using UnityEngine;
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
public class SpeechManager : BaseController
{
    // public VirtualManController controller;
    private static readonly bool ShouldLoop = true;
    private static readonly int MaxLength = 10;
    private static readonly int Frequency = 44100;
    string _microphone = null;
    private AudioClip recordingClip = null;
    private AudioSource _audioSource;
    internal static SpeechManager Instance = null;
    private SpeechSynthesizer synthesizer = new SpeechSynthesizer(AzureAuth.SpeechConfig);
    private int testCnt = 0;

    //Is virHuman speaking
    private static bool isSpeaking;
    public static bool IsSpeaking => isSpeaking;
    public Speech2BlendshapeController speech2BlendshapeController;

    private bool IsRecognizing;

    public override void OnRegister()
    {
        base.OnRegister();
        Init();
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
    }

    void Init()
    {
        Instance = this;
        _audioSource = GetComponent<AudioSource>();
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
        synthesizer.SynthesisStarted += (self, args) =>
        {
            Debug.Log("text SynthesisStarted: Synthesis completed: {args.Result.Reason}");
            isSpeaking = true;
        };
        synthesizer.SynthesisCompleted += (self, args) =>
        {
            Debug.Log("text SynthesisCompleted: Synthesis completed: " + args.Result.Reason);
            isSpeaking = false;
        };
        synthesizer.SynthesisCanceled += (self, args) =>
        {
            Debug.Log("text SynthesisCanceled: Synthesis completed: " + args.Result.Reason);
            isSpeaking = false;
        };

        synthesizer.VisemeReceived += (sender, e) =>
        {

            Debug.Log($"[Viseme] ID: {e.VisemeId}, Time: {e.AudioOffset / 10000} ms, Animation Length: {e.Animation.Length}");
            string animationJson = e.Animation;

            // 必须在主线程设置 BlendShape（Unity 限制）
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                speech2BlendshapeController.SetVisemeBlendShapeWeight(e.VisemeId, 80f);
            });
        };
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
        if (Instance != null)
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
                // controller.LongTalk();
                TextBubble.SetGlobalText(text);
            });
        };
        synthesis.SynthesisCompleted += (self, args) =>
        {
            Debug.Log($"[{nameof(SpeakText)}]: Synthesis completed: {args.Result.Reason}", gameObject);
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                TextBubble.SetGlobalText(string.Empty);
                // controller.StopTalk();
            });
        };
        var result = await synthesis.SpeakTextAsync(text.Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "")).ConfigureAwait(false);
        //var result = await synthesis.SpeakTextAsync(text).ConfigureAwait(false);
        MainThreadDispatcher.InvokeOnMainThread(() =>
        {
            TextBubble.SetGlobalText(string.Empty);
            // controller.StopTalk();
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
        await synthesizer.StopSpeakingAsync();
        Debug.Log("Msg: " + text + "prepare");

        var result = await synthesizer.SpeakTextAsync(text.Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "")).ConfigureAwait(false);
        Debug.Log("Msg: " + text + "result" + result.AudioData.Length);
        onSpeakComplete?.Invoke();
    }

    public async Task ForceStopSpeak()
    {
        await synthesizer.StopSpeakingAsync();
        //isSpeaking = false;
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
        synthesizer?.StopSpeakingAsync();
        Instance.StopAllCoroutines();
    }
}
