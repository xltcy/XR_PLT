using System;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class MimoTTSSynthesizerBackend : ISpeechSynthesizerBackend
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

        var config = LLMModelConfig.Speech.MimoTTS;
        string cleanText = SpeechManager.CleanSpeechText(text);
        string voiceName = voiceGender == SpeechSynthesizerController.SpeechVoiceGender.Female
            ? config.FemaleVoiceName
            : config.MaleVoiceName;
        NetworkResponse response = null;
        bool requestSuccess = false;
        var requestParam = new Network.RequestParam.MimoTTS.RequestParam(config, cleanText, voiceName);

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
            Debug.LogError($"[SpeechSynthesizerController] MimoTTS request failed: {response?.statusCode} - {response?.error}, {response?.rawResponse}");
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        byte[] audioData;
        try
        {
            audioData = ExtractMimoTTSAudioData(response.rawResponse);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SpeechSynthesizerController] MimoTTS response parse failed: {e.Message}\n{response?.rawResponse}");
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        AudioClip clip = SpeechSynthesizerBackendUtils.TryCreateWavAudioClip(audioData, "MimoTTS");
        yield return SpeechSynthesizerBackendUtils.PlayClip(host, clip, onSpeakComplete, tcs, "MimoTTS");
        if (activeSpeakTask == tcs)
        {
            activeSpeakTask = null;
        }
    }

    private static byte[] ExtractMimoTTSAudioData(string responseText)
    {
        var json = JObject.Parse(responseText);
        JToken audioToken =
            json.SelectToken("choices[0].message.audio.data") ??
            json.SelectToken("choices[0].message.audio") ??
            json.SelectToken("choices[0].audio.data") ??
            json.SelectToken("choices[0].audio") ??
            json.SelectToken("audio.data") ??
            json.SelectToken("audio") ??
            json.SelectToken("data");

        string base64 = audioToken?.Type == JTokenType.String ? audioToken.Value<string>() : null;
        if (string.IsNullOrEmpty(base64))
        {
            throw new InvalidOperationException("No base64 audio field found in MimoTTS response.");
        }

        int commaIndex = base64.IndexOf(',');
        if (commaIndex >= 0)
        {
            base64 = base64.Substring(commaIndex + 1);
        }

        return Convert.FromBase64String(base64);
    }
}
