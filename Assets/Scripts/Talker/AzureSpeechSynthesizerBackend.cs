using System;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using UnityEngine;

public class AzureSpeechSynthesizerBackend : ISpeechSynthesizerBackend
{
    private const int StopSpeakingTimeoutMs = 1000;
    // Keep Azure formal but less stiff for PPT-style lab introductions.
    private const string AzureFemalePresentationStyle = "calm";
    private const string AzureMalePresentationStyle = "narration-professional";
    private const string AzureFemaleStyleDegree = "0.45";
    private const string AzureMaleStyleDegree = "0.65";
    private const string AzureTargetRate = "-20%";
    private const string ShortPause = "180ms";
    private const string LongPause = "420ms";
    private const string ParagraphPause = "650ms";
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

            Debug.Log("Msg: " + text + "prepare");
            var result = await SpeakWithAzureSsml(synthesizer, text).ConfigureAwait(false);
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

        SpeechSynthesizer target = synthesizer;
        synthesizer = null;
        _ = StopAndDisposeSynthesizer(target);
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

    private static async Task StopAndDisposeSynthesizer(SpeechSynthesizer target)
    {
        if (target == null)
        {
            return;
        }

        try
        {
            await StopSynthesizerWithTimeout(target).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                target.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SpeechSynthesizerController] Dispose Azure synthesizer failed: {e.Message}");
            }
        }
    }

    private static async Task<SpeechSynthesisResult> SpeakWithAzureSsml(SpeechSynthesizer target, string text)
    {
        string styledSsml = BuildAzureSsml(text, includeHumanLikeStyle: true);
        var result = await target.SpeakSsmlAsync(styledSsml).ConfigureAwait(false);
        if (result.Reason != ResultReason.Canceled)
        {
            return result;
        }

        string plainSsml = BuildAzureSsml(text, includeHumanLikeStyle: false);
        return await target.SpeakSsmlAsync(plainSsml).ConfigureAwait(false);
    }

    public static string BuildAzureSsml(string text, bool includeHumanLikeStyle)
    {
        string voiceName = AzureAuth.SpeechSynthesisVoiceName;
        string naturalText = BuildNaturalTextSsml(text);
        string prosody = $"<prosody rate=\"{AzureTargetRate}\">{naturalText}</prosody>";
        string body = includeHumanLikeStyle
            ? $"<mstts:express-as style=\"{GetAzurePresentationStyle(voiceName)}\" styledegree=\"{GetAzureStyleDegree(voiceName)}\">{prosody}</mstts:express-as>"
            : prosody;

        return $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"zh-CN\"><voice name=\"{voiceName}\">{body}</voice></speak>";
    }

    private static string GetAzurePresentationStyle(string voiceName)
    {
        return voiceName == AzureAuth.FemaleVoiceName ? AzureFemalePresentationStyle : AzureMalePresentationStyle;
    }

    private static string GetAzureStyleDegree(string voiceName)
    {
        return voiceName == AzureAuth.FemaleVoiceName ? AzureFemaleStyleDegree : AzureMaleStyleDegree;
    }

    private static string BuildNaturalTextSsml(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length + 128);
        bool previousWasBreak = false;
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (character == '\r')
            {
                continue;
            }

            if (character == '\n')
            {
                AppendBreak(builder, ParagraphPause, ref previousWasBreak);
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            if (IsDecimalPoint(text, i))
            {
                builder.Append('\u70B9');
                previousWasBreak = false;
                continue;
            }

            builder.Append(SecurityElement.Escape(character.ToString()) ?? string.Empty);
            previousWasBreak = false;

            if (IsLongPausePunctuation(character))
            {
                AppendBreak(builder, LongPause, ref previousWasBreak);
            }
            else if (IsShortPausePunctuation(character))
            {
                AppendBreak(builder, ShortPause, ref previousWasBreak);
            }
        }

        return builder.ToString();
    }

    private static void AppendBreak(StringBuilder builder, string time, ref bool previousWasBreak)
    {
        if (previousWasBreak)
        {
            return;
        }

        builder.Append("<break time=\"");
        builder.Append(time);
        builder.Append("\"/>");
        previousWasBreak = true;
    }

    private static bool IsShortPausePunctuation(char character)
    {
        return character == ',' || character == ':' || character == ';'
            || character == '\uFF0C' || character == '\u3001' || character == '\uFF1A' || character == '\uFF1B';
    }

    private static bool IsLongPausePunctuation(char character)
    {
        return character == '\u3002' || character == '\uFF1F' || character == '\uFF01';
    }

    private static bool IsDecimalPoint(string text, int index)
    {
        if (index <= 0 || index >= text.Length - 1)
        {
            return false;
        }

        char character = text[index];
        return (character == '.' || character == '\uFF0E')
            && char.IsDigit(text[index - 1])
            && char.IsDigit(text[index + 1]);
    }
}
