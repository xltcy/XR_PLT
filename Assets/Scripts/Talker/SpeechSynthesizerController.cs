using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 统一管理语音合成 provider。SpeechManager 负责外部入口和播放宿主，本类负责选择具体合成器。
/// </summary>
public class SpeechSynthesizerController : BaseController
{
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

    // 每个 provider 封装成独立 backend，避免新增模型时继续堆到 SpeechManager。
    private readonly Dictionary<SpeechSynthesisMode, ISpeechSynthesizerBackend> synthesizers = new Dictionary<SpeechSynthesisMode, ISpeechSynthesizerBackend>();
    private bool isDisposing;

    public SpeechSynthesisMode SynthesisMode { get; private set; } = SpeechSynthesisMode.CosyVoice;
    public SpeechVoiceGender VoiceGender { get; private set; } = SpeechVoiceGender.Male;

    public override void OnRegister()
    {
        isDisposing = false;
        base.OnRegister();
        EnsureSynthesizers();
    }

    public override void OnUnregister()
    {
        DisposeSynthesizers();
        base.OnUnregister();
    }

    public void SetSynthesisMode(SpeechSynthesisMode mode)
    {
        if (isDisposing)
        {
            return;
        }

        EnsureSynthesizers();
        SynthesisMode = mode;
        ApplyGenderToSynthesizers();
        Debug.Log($"[SpeechSynthesizerController] Speech synthesis mode: {SynthesisMode}");
    }

    public void SetVoiceGender(SpeechVoiceGender gender)
    {
        if (isDisposing)
        {
            return;
        }

        EnsureSynthesizers();
        VoiceGender = gender;
        ApplyGenderToSynthesizers();
        Debug.Log($"[SpeechSynthesizerController] Speech voice gender: {VoiceGender}");
    }

    public void SetVoiceByAzureVoiceName(string voiceName)
    {
        if (string.IsNullOrEmpty(voiceName))
        {
            return;
        }

        SetVoiceGender(voiceName == AzureAuth.FemaleVoiceName ? SpeechVoiceGender.Female : SpeechVoiceGender.Male);
    }

    public async Task SpeakText(SpeechManager host, string text, Action onSpeakComplete = null)
    {
        if (isDisposing)
        {
            onSpeakComplete?.Invoke();
            return;
        }

        EnsureSynthesizers();
        if (host == null || host.IsSpeechShuttingDown)
        {
            onSpeakComplete?.Invoke();
            return;
        }

        if (!synthesizers.TryGetValue(SynthesisMode, out var synthesizer))
        {
            Debug.LogWarning($"[SpeechSynthesizerController] Missing synthesizer for {SynthesisMode}, fallback to Azure.");
            if (!synthesizers.TryGetValue(SpeechSynthesisMode.Azure, out synthesizer))
            {
                Debug.LogError("[SpeechSynthesizerController] Azure fallback synthesizer is not available.");
                onSpeakComplete?.Invoke();
                return;
            }
        }

        await synthesizer.SpeakText(host, text, onSpeakComplete).ConfigureAwait(false);
    }

    public async Task StopSpeaking(SpeechManager host)
    {
        if (isDisposing)
        {
            return;
        }

        EnsureSynthesizers();
        foreach (var synthesizer in synthesizers.Values)
        {
            await synthesizer.StopSpeaking().ConfigureAwait(false);
        }

        MainThreadDispatcher.InvokeOnMainThread(() =>
        {
            if (host != null)
            {
                host.FinishExternalSpeech(null);
            }
        });
    }

    /// <summary>
    /// PlayMode 退出、场景销毁或宿主 SpeechManager 关闭时调用。
    /// 这里不等待 provider 的网络/SDK stop 任务，避免 Unity 停止 Play 后仍有语音后台任务阻塞下一次 domain reload。
    /// </summary>
    public void ShutdownImmediately()
    {
        DisposeSynthesizers();
    }

    private void ApplyGenderToSynthesizers()
    {
        // 性别由 introduce UI 的形象切换驱动；各 provider 在自己的 backend 中映射具体 voice。
        foreach (var synthesizer in synthesizers.Values)
        {
            synthesizer.SetVoiceGender(VoiceGender);
        }
    }

    private void EnsureSynthesizers()
    {
        if (isDisposing)
        {
            return;
        }

        if (!synthesizers.ContainsKey(SpeechSynthesisMode.Azure))
        {
            synthesizers[SpeechSynthesisMode.Azure] = new AzureSpeechSynthesizerBackend();
        }

        if (!synthesizers.ContainsKey(SpeechSynthesisMode.MimoTTS))
        {
            synthesizers[SpeechSynthesisMode.MimoTTS] = new MimoTTSSynthesizerBackend();
        }

        if (!synthesizers.ContainsKey(SpeechSynthesisMode.CosyVoice))
        {
            synthesizers[SpeechSynthesisMode.CosyVoice] = new CosyVoiceSynthesizerBackend();
        }

        ApplyGenderToSynthesizers();
    }

    private void DisposeSynthesizers()
    {
        if (isDisposing)
        {
            return;
        }

        isDisposing = true;
        foreach (var synthesizer in synthesizers.Values)
        {
            synthesizer.Dispose();
        }

        synthesizers.Clear();
    }

    private void OnApplicationQuit()
    {
        DisposeSynthesizers();
    }

    private void OnDestroy()
    {
        DisposeSynthesizers();
    }
}
