using System;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class CosyVoiceSynthesizerBackend : ISpeechSynthesizerBackend
{
    private SpeechSynthesizerController.SpeechVoiceGender voiceGender = SpeechSynthesizerController.SpeechVoiceGender.Male;
    private TaskCompletionSource<bool> activeSpeakTask;
    private bool isDisposed;

    public void SetVoiceGender(SpeechSynthesizerController.SpeechVoiceGender gender) => voiceGender = gender;

    public Task SpeakText(SpeechManager host, string text, Action onSpeakComplete)
    {
        if (isDisposed)
        {
            return Task.CompletedTask;
        }

        activeSpeakTask?.TrySetResult(false);
        var tcs = new TaskCompletionSource<bool>();
        activeSpeakTask = tcs;
        MainThreadDispatcher.InvokeOnMainThread(() =>
        {
            if (isDisposed || host == null || host.IsSpeechShuttingDown)
            {
                tcs.TrySetResult(false);
                return;
            }

            host.StartCoroutine(SpeakCoroutine(host, text, onSpeakComplete, tcs));
        });
        return tcs.Task;
    }

    public Task StopSpeaking()
    {
        activeSpeakTask?.TrySetResult(false);
        activeSpeakTask = null;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        isDisposed = true;
        activeSpeakTask?.TrySetResult(false);
        activeSpeakTask = null;
    }

    private IEnumerator SpeakCoroutine(SpeechManager host, string text, Action onSpeakComplete, TaskCompletionSource<bool> tcs)
    {
        if (isDisposed)
        {
            tcs.TrySetResult(false);
            yield break;
        }

        var config = LLMModelConfig.Speech.CosyVoice;
        string voiceName = voiceGender == SpeechSynthesizerController.SpeechVoiceGender.Female
            ? config.FemaleVoiceName
            : config.MaleVoiceName;
        NetworkResponse response = null;
        bool requestSuccess = false;
        var requestParam = new Network.RequestParam.CosyVoiceTTS.RequestParam(config, SpeechManager.CleanSpeechText(text), voiceName);

        host.BeginExternalSpeech(text);
        yield return SpeechSynthesizerBackendUtils.SendNetworkRequest(requestParam, (success, networkResponse) =>
        {
            requestSuccess = success;
            response = networkResponse;
        });

        if (isDisposed || host.IsSpeechShuttingDown)
        {
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        if (!requestSuccess)
        {
            if (response?.statusCode == 401)
            {
                Debug.LogError($"[SpeechSynthesizerController] CosyVoice authorization failed. Check DashScope/Bailian API key, region, and model permission. Response: {response.rawResponse}");
            }
            else
            {
                Debug.LogError($"[SpeechSynthesizerController] CosyVoice request failed: {response?.statusCode} - {response?.error}, {response?.rawResponse}");
            }

            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        string audioUrl;
        try
        {
            audioUrl = JObject.Parse(response.rawResponse).SelectToken("output.audio.url")?.Value<string>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SpeechSynthesizerController] CosyVoice response parse failed: {e.Message}\n{response?.rawResponse}");
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        if (string.IsNullOrEmpty(audioUrl))
        {
            Debug.LogError($"[SpeechSynthesizerController] CosyVoice response has no output.audio.url.\n{response?.rawResponse}");
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        audioUrl = NormalizeAudioUrl(audioUrl);
        using var audioRequest = UnityWebRequest.Get(audioUrl);
        yield return audioRequest.SendWebRequest();

        if (isDisposed || host.IsSpeechShuttingDown)
        {
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        if (audioRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[SpeechSynthesizerController] CosyVoice audio download failed: {audioRequest.error}, url: {audioUrl}");
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        byte[] audioData = audioRequest.downloadHandler?.data;
        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogError($"[SpeechSynthesizerController] CosyVoice audio download returned empty data, url: {audioUrl}");
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        AudioClip clip = SpeechSynthesizerBackendUtils.TryCreateWavAudioClip(audioData, "CosyVoice");
        yield return SpeechSynthesizerBackendUtils.PlayClip(host, clip, onSpeakComplete, tcs, "CosyVoice");
        if (activeSpeakTask == tcs)
        {
            activeSpeakTask = null;
        }
    }

    private static string NormalizeAudioUrl(string audioUrl)
    {
        if (string.IsNullOrEmpty(audioUrl))
        {
            return audioUrl;
        }

        return audioUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? "https://" + audioUrl.Substring("http://".Length)
            : audioUrl;
    }
}
