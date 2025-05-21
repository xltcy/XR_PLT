using UnityEngine;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

/***
 * This file will be updated in future.
 * BubbleText need to be modified to a prefab.
 * Use SayFromStr to speak a sentence from string param.
 * Function SpeakText & OnlySpeakText will be set private in future versions.
 */
public class SpeechManager : MonoBehaviour
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

private bool IsRecognizing;
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        _audioSource = GetComponent<AudioSource>();
        if (Microphone.devices.IsEmpty())
        {
            TextBubble.SetGlobalText("û����˷�");
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

    void ProcessRecognizedText(string text)
    {
        if (text.Contains("���ת"))
        {
            //controlled.GetComponent<IHumanControl>().TurnBack();
            this.RunTask(SpeakText("�õ�"));
        }
    }

    public static void SayFromStr(string str)
    {
        if (Instance != null)
        {
            var speakTask = Instance.OnlySpeakText(str);
            Instance.RunTask(speakTask);
            //isSpeaking = true;
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

    public async Task OnlySpeakText(string text)
    {
        if (text == null || text == "")
        {
            Debug.Log("Msg: Empty String");
            return;
        }
        await synthesizer.StopSpeakingAsync();
        Debug.Log("Msg: " + text + "prepare");

        var result = await synthesizer.SpeakTextAsync(text.Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "")).ConfigureAwait(false);
        Debug.Log("Msg: " + text + "result" + result.AudioData.Length);
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
        synthesizer.StopSpeakingAsync();
    }
}
