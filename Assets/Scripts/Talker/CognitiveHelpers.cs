using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using UnityEngine;

public static class CognitiveHelpers
{
    public static async Task<string> ContinuousRecognizeString(this SpeechRecognizer sr)
    {
        var text = "";
        var stopRecognition = new TaskCompletionSource<int>();
        var recognizedText = new TaskCompletionSource<string>();
        sr.Recognizing += (s, e) =>
        {
            text = e.Result.Text;
            Debug.Log($"[SR]: Received {e.Result.Text}");
        };
        sr.Recognized += (s, e) =>
        {
            Debug.Log($"[SR]: Successfully received: {e.Result.Text}");
            recognizedText.TrySetResult(e.Result.Text);
            stopRecognition.TrySetResult(0);
        };
        sr.SessionStopped += (_s, _e) =>
        {
            Debug.Log("[SR]: Session stopped.");
            stopRecognition.TrySetResult(0);
        };
        sr.Canceled += (_s, e) =>
        {
            Debug.LogWarning($"[SR]: Cancelled: {e.Reason}");
            stopRecognition.TrySetResult(1);
            recognizedText.TrySetResult("");
        };
        Debug.Log("[SR]: Starting recognizing.");
        await sr.StartContinuousRecognitionAsync().ConfigureAwait(false);
        
        await stopRecognition.Task.ConfigureAwait(false);
        if (!recognizedText.Task.IsCompleted) recognizedText.SetCanceled();
        await sr.StopContinuousRecognitionAsync().ConfigureAwait(false);
        Debug.Log($"[SR]: Stopping recognition with {text}.");
        return await recognizedText.Task.ConfigureAwait(false);
    }

    public static async Task<string> RecognizeOnce(this SpeechRecognizer sr)
    {
        var result = await sr.RecognizeOnceAsync();
        return result.Text;
    }
}
