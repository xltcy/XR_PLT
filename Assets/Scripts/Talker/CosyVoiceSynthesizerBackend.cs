using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class CosyVoiceSynthesizerBackend : ISpeechSynthesizerBackend
{
    private const int StreamStartBufferMs = 250;
    private const int MaxStreamClipSeconds = 600;

    private SpeechSynthesizerController.SpeechVoiceGender voiceGender = SpeechSynthesizerController.SpeechVoiceGender.Male;
    private TaskCompletionSource<bool> activeSpeakTask;
    private UnityWebRequest activeRequest;
    private StreamingPcmAudioPlayer activeStreamPlayer;
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
        activeRequest?.Abort();
        activeRequest = null;
        activeStreamPlayer?.Stop();
        activeStreamPlayer = null;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        isDisposed = true;
        activeSpeakTask?.TrySetResult(false);
        activeSpeakTask = null;
        activeRequest?.Abort();
        activeRequest = null;
        activeStreamPlayer?.Stop();
        activeStreamPlayer = null;
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
        var requestParam = new Network.RequestParam.CosyVoiceTTS.RequestParam(config, SpeechManager.CleanSpeechText(text), voiceName, stream: true);

        host.BeginExternalSpeech(text);
        yield return SpeakStreamingCoroutine(host, requestParam, onSpeakComplete, tcs, config.SampleRate <= 0 ? 24000 : config.SampleRate);
        if (activeSpeakTask == tcs)
        {
            activeSpeakTask = null;
        }
    }

    private IEnumerator SpeakStreamingCoroutine(
        SpeechManager host,
        Network.RequestParam.CosyVoiceTTS.RequestParam requestParam,
        Action onSpeakComplete,
        TaskCompletionSource<bool> tcs,
        int sampleRate)
    {
        var audioSource = host.SpeechAudioSource;
        if (audioSource == null)
        {
            Debug.LogError("[SpeechSynthesizerController] SpeechManager has no AudioSource.");
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        string streamError = null;
        var streamPlayer = new StreamingPcmAudioPlayer(audioSource, sampleRate);
        var downloadHandler = new CosyVoiceSseDownloadHandler(
            pcmData => streamPlayer.EnqueuePcm16(pcmData),
            error => streamError = error);
        activeStreamPlayer = streamPlayer;

        using var request = CreateStreamingRequest(requestParam, downloadHandler);
        activeRequest = request;
        Debug.Log($"[SpeechSynthesizerController] CosyVoice streaming request started: {request.url}");
        var asyncOperation = request.SendWebRequest();
        bool playbackStarted = false;
        int startBufferSamples = Mathf.Max(1, sampleRate * StreamStartBufferMs / 1000);

        while (!asyncOperation.isDone)
        {
            if (isDisposed || host.IsSpeechShuttingDown || tcs.Task.IsCompleted)
            {
                request.Abort();
                streamPlayer.Stop();
                host.FinishExternalSpeech(null);
                tcs.TrySetResult(false);
                yield break;
            }

            if (!playbackStarted && streamPlayer.BufferedSampleCount >= startBufferSamples)
            {
                streamPlayer.Play();
                playbackStarted = true;
            }

            yield return null;
        }

        activeRequest = null;
        if (isDisposed || host.IsSpeechShuttingDown || tcs.Task.IsCompleted)
        {
            streamPlayer.Stop();
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (request.responseCode == 401)
            {
                Debug.LogError($"[SpeechSynthesizerController] CosyVoice authorization failed. Check DashScope/Bailian API key, region, and model permission. Response: {downloadHandler.ResponseText}");
            }
            else
            {
                Debug.LogError($"[SpeechSynthesizerController] CosyVoice streaming request failed: {(int)request.responseCode} - {request.error}, {downloadHandler.ResponseText}");
            }

            streamPlayer.Stop();
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        if (!string.IsNullOrEmpty(streamError))
        {
            Debug.LogError($"[SpeechSynthesizerController] CosyVoice streaming response parse failed: {streamError}");
            streamPlayer.Stop();
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        if (!playbackStarted)
        {
            if (streamPlayer.BufferedSampleCount <= 0)
            {
                Debug.LogError($"[SpeechSynthesizerController] CosyVoice streaming response has no audio data. Response: {downloadHandler.ResponseText}");
                streamPlayer.Stop();
                host.FinishExternalSpeech(null);
                tcs.TrySetResult(false);
                yield break;
            }

            streamPlayer.Play();
            playbackStarted = true;
        }

        float emptyBufferStartTime = -1f;
        while (!isDisposed && !host.IsSpeechShuttingDown)
        {
            if (streamPlayer.BufferedSampleCount <= 0)
            {
                if (emptyBufferStartTime < 0f)
                {
                    emptyBufferStartTime = Time.realtimeSinceStartup;
                }
                else if (Time.realtimeSinceStartup - emptyBufferStartTime > 0.5f)
                {
                    break;
                }
            }
            else
            {
                emptyBufferStartTime = -1f;
            }

            yield return null;
        }

        streamPlayer.Stop();
        activeStreamPlayer = null;
        if (isDisposed || host.IsSpeechShuttingDown)
        {
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        Debug.Log(downloadHandler.CreateDiagnosticSummary());
        host.FinishExternalSpeech(onSpeakComplete);
        tcs.TrySetResult(true);
    }

    private static UnityWebRequest CreateStreamingRequest(BaseRequestParam requestParam, DownloadHandler downloadHandler)
    {
        var request = new UnityWebRequest(requestParam.url, requestParam.method);
        string json = JsonConvert.SerializeObject(requestParam.requestData);
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = downloadHandler;
        request.timeout = requestParam.timeout;
        foreach (var header in requestParam.headers)
        {
            request.SetRequestHeader(header.Key, header.Value);
        }

        return request;
    }

    private sealed class CosyVoiceSseDownloadHandler : DownloadHandlerScript
    {
        private readonly Action<byte[]> onAudioData;
        private readonly Action<string> onError;
        private readonly StringBuilder pendingText = new StringBuilder();
        private readonly StringBuilder responseText = new StringBuilder();
        private readonly HashSet<string> responseFieldNames = new HashSet<string>();
        private readonly HashSet<string> visemeLikeFieldNames = new HashSet<string>();
        private string firstSanitizedPayload;
        private int audioChunkCount;

        public string ResponseText => responseText.ToString();

        public CosyVoiceSseDownloadHandler(Action<byte[]> onAudioData, Action<string> onError)
            : base(new byte[8192])
        {
            this.onAudioData = onAudioData;
            this.onError = onError;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength <= 0)
            {
                return true;
            }

            string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
            responseText.Append(chunk);
            pendingText.Append(chunk);
            ProcessPendingEvents();
            return true;
        }

        protected override void CompleteContent()
        {
            ProcessEvent(pendingText.ToString());
            pendingText.Length = 0;
        }

        private void ProcessPendingEvents()
        {
            while (true)
            {
                string text = pendingText.ToString();
                int separatorIndex = text.IndexOf("\n\n", StringComparison.Ordinal);
                int separatorLength = 2;
                if (separatorIndex < 0)
                {
                    separatorIndex = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    separatorLength = 4;
                }

                if (separatorIndex < 0)
                {
                    return;
                }

                string eventText = text.Substring(0, separatorIndex);
                pendingText.Remove(0, separatorIndex + separatorLength);
                ProcessEvent(eventText);
            }
        }

        private void ProcessEvent(string eventText)
        {
            if (string.IsNullOrWhiteSpace(eventText))
            {
                return;
            }

            var dataBuilder = new StringBuilder();
            string[] lines = eventText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                dataBuilder.Append(line.Substring("data:".Length).Trim());
            }

            string json = dataBuilder.ToString();
            if (string.IsNullOrWhiteSpace(json) || json == "[DONE]")
            {
                return;
            }

            try
            {
                var payload = JObject.Parse(json);
                CollectFieldNames(payload);
                if (firstSanitizedPayload == null)
                {
                    firstSanitizedPayload = SanitizePayload(payload).ToString(Newtonsoft.Json.Formatting.None);
                }

                var audioData = payload.SelectToken("output.audio.data")?.Value<string>();
                if (!string.IsNullOrEmpty(audioData))
                {
                    audioChunkCount++;
                    onAudioData?.Invoke(Convert.FromBase64String(audioData));
                }
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
        }

        public string CreateDiagnosticSummary()
        {
            string visemeFields = visemeLikeFieldNames.Count > 0
                ? string.Join(", ", visemeLikeFieldNames)
                : "none";
            string allFields = responseFieldNames.Count > 0
                ? string.Join(", ", responseFieldNames)
                : "none";
            string firstPayload = string.IsNullOrEmpty(firstSanitizedPayload)
                ? "none"
                : firstSanitizedPayload;

            return $"[SpeechSynthesizerController] CosyVoice stream diagnostics: audioChunks={audioChunkCount}, visemeLikeFields={visemeFields}, fields={allFields}, firstPayload={firstPayload}";
        }

        private void CollectFieldNames(JToken token)
        {
            if (token == null)
            {
                return;
            }

            if (token.Type == JTokenType.Property)
            {
                var property = (JProperty)token;
                responseFieldNames.Add(property.Name);
                if (IsVisemeLikeFieldName(property.Name))
                {
                    visemeLikeFieldNames.Add(property.Name);
                }

                CollectFieldNames(property.Value);
                return;
            }

            foreach (JToken child in token.Children())
            {
                CollectFieldNames(child);
            }
        }

        private static bool IsVisemeLikeFieldName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            string normalized = fieldName.ToLowerInvariant();
            return normalized.Contains("viseme")
                || normalized.Contains("phoneme")
                || normalized.Contains("alignment")
                || normalized.Contains("timestamp")
                || normalized.Contains("audio_offset")
                || normalized.Contains("offset");
        }

        private static JToken SanitizePayload(JToken payload)
        {
            JToken clone = payload.DeepClone();
            JToken audioData = clone.SelectToken("output.audio.data");
            if (audioData != null)
            {
                audioData.Replace("<base64 audio omitted>");
            }

            return clone;
        }
    }

    private sealed class StreamingPcmAudioPlayer
    {
        private readonly AudioSource audioSource;
        private readonly int sampleRate;
        private readonly Queue<float> sampleQueue = new Queue<float>();
        private readonly object queueLock = new object();
        private AudioClip clip;

        public StreamingPcmAudioPlayer(AudioSource audioSource, int sampleRate)
        {
            this.audioSource = audioSource;
            this.sampleRate = sampleRate;
        }

        public int BufferedSampleCount
        {
            get
            {
                lock (queueLock)
                {
                    return sampleQueue.Count;
                }
            }
        }

        public bool HasBufferedAudio => BufferedSampleCount > 0;

        public void EnqueuePcm16(byte[] pcmData)
        {
            if (pcmData == null || pcmData.Length < 2)
            {
                return;
            }

            lock (queueLock)
            {
                int evenLength = pcmData.Length - pcmData.Length % 2;
                for (int i = 0; i < evenLength; i += 2)
                {
                    short value = (short)(pcmData[i] | (pcmData[i + 1] << 8));
                    sampleQueue.Enqueue(value / 32768f);
                }
            }
        }

        public void Play()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.Stop();
            if (clip != null)
            {
                UnityEngine.Object.Destroy(clip);
            }

            clip = AudioClip.Create("CosyVoiceStreaming", sampleRate * MaxStreamClipSeconds, 1, sampleRate, true, OnAudioRead);
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log($"[SpeechSynthesizerController] CosyVoice streaming audio playing. sampleRate={sampleRate}");
        }

        public void Stop()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                if (audioSource.clip == clip)
                {
                    audioSource.clip = null;
                }
            }

            if (clip != null)
            {
                UnityEngine.Object.Destroy(clip);
                clip = null;
            }

            lock (queueLock)
            {
                sampleQueue.Clear();
            }
        }

        private void OnAudioRead(float[] data)
        {
            lock (queueLock)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = sampleQueue.Count > 0 ? sampleQueue.Dequeue() : 0f;
                }
            }
        }
    }
}
