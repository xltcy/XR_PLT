using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using UnityEngine;

public class AzureSpeechSynthesizerBackend : ISpeechSynthesizerBackend
{
    private const int StopSpeakingTimeoutMs = 1000;
    private SpeechSynthesizer synthesizer;
    private SpeechManager host;
    private TaskCompletionSource<bool> activeSpeakTask;
    private bool isDisposed;

    public void SetVoiceGender(SpeechSynthesizerController.SpeechVoiceGender gender)
    {
        if (isDisposed)
        {
            return;
        }

        string voiceName = gender == SpeechSynthesizerController.SpeechVoiceGender.Female
            ? AzureAuth.FemaleVoiceName
            : AzureAuth.MaleVoiceName;
        if (voiceName == AzureAuth.SpeechSynthesisVoiceName)
        {
            return;
        }

        AzureAuth.SetSpeechSynthesisVoiceName(voiceName);
        RebuildSynthesizer();
    }

    public async Task SpeakText(SpeechManager speechHost, string text, Action onSpeakComplete)
    {
        if (isDisposed)
        {
            return;
        }

        activeSpeakTask?.TrySetResult(false);
        var tcs = new TaskCompletionSource<bool>();
        activeSpeakTask = tcs;
        host = speechHost;
        EnsureSynthesizer();
        if (synthesizer == null || speechHost.IsSpeechShuttingDown)
        {
            tcs.TrySetResult(false);
            return;
        }

        _ = RunSpeakTextAsync(speechHost, text, onSpeakComplete, tcs);
        await tcs.Task.ConfigureAwait(false);
    }

    private async Task RunSpeakTextAsync(SpeechManager speechHost, string text, Action onSpeakComplete, TaskCompletionSource<bool> tcs)
    {
        try
        {
            await synthesizer.StopSpeakingAsync().ConfigureAwait(false);
            if (isDisposed || speechHost.IsSpeechShuttingDown)
            {
                tcs.TrySetResult(false);
                return;
            }

            string cleanText = SpeechManager.CleanSpeechText(text);
            Debug.Log("Msg: " + text + "prepare");
            var result = await synthesizer.SpeakTextAsync(cleanText).ConfigureAwait(false);
            if (isDisposed || speechHost.IsSpeechShuttingDown)
            {
                tcs.TrySetResult(false);
                return;
            }

            Debug.Log("Msg: " + text + "result" + result.AudioData.Length);
            if (onSpeakComplete != null)
            {
                MainThreadDispatcher.InvokeOnMainThread(onSpeakComplete);
            }

            tcs.TrySetResult(true);
        }
        catch (ObjectDisposedException)
        {
            tcs.TrySetResult(false);
        }
        catch (Exception e)
        {
            if (!isDisposed && !speechHost.IsSpeechShuttingDown)
            {
                Debug.LogError($"[SpeechSynthesizerController] Azure speak text failed: {e}");
            }

            tcs.TrySetResult(false);
        }
        finally
        {
            if (activeSpeakTask == tcs)
            {
                activeSpeakTask = null;
            }
        }
    }

    public async Task StopSpeaking()
    {
        activeSpeakTask?.TrySetResult(false);
        activeSpeakTask = null;
        if (synthesizer != null)
        {
            await StopSynthesizerWithTimeout(synthesizer).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        isDisposed = true;
        activeSpeakTask?.TrySetResult(false);
        activeSpeakTask = null;
        host = null;

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
            Debug.LogWarning($"[SpeechSynthesizerController] Dispose Azure synthesizer failed: {e.Message}");
        }

        synthesizer = null;
    }

    private void EnsureSynthesizer()
    {
        if (isDisposed || synthesizer != null)
        {
            return;
        }

        synthesizer = new SpeechSynthesizer(AzureAuth.SpeechConfig);
        synthesizer.SynthesisStarted += (self, args) =>
        {
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                if (host != null && !host.IsSpeechShuttingDown)
                {
                    host.BeginExternalSpeech(null);
                }
            });
        };
        synthesizer.SynthesisCompleted += (self, args) =>
        {
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                if (host != null)
                {
                    host.EndExternalSpeechSchedule();
                }
            });
        };
        synthesizer.SynthesisCanceled += (self, args) =>
        {
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                if (host != null)
                {
                    host.EndExternalSpeechSchedule();
                }
            });
        };
        synthesizer.VisemeReceived += (sender, e) =>
        {
            // Azure 会返回 viseme 时间线，可以驱动更精确的口型；HTTP TTS provider 只能用播放周期兜底。
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                if (host != null && !host.IsSpeechShuttingDown && host.speech2BlendshapeController != null)
                {
                    host.speech2BlendshapeController.ScheduleViseme(e.VisemeId, e.AudioOffset / 10000000f);
                }
            });
        };
    }

    private void RebuildSynthesizer()
    {
        Dispose();
        isDisposed = false;
        EnsureSynthesizer();
    }

    private static async Task StopSynthesizerWithTimeout(SpeechSynthesizer target)
    {
        try
        {
            var stopTask = target.StopSpeakingAsync();
            var completedTask = await Task.WhenAny(stopTask, Task.Delay(StopSpeakingTimeoutMs)).ConfigureAwait(false);
            if (completedTask != stopTask)
            {
                Debug.LogWarning("[SpeechSynthesizerController] Stop Azure speaking timed out.");
            }
            else
            {
                await stopTask.ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SpeechSynthesizerController] Stop Azure speaking failed: {e.Message}");
        }
    }
}
