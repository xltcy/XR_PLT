using System;
using System.Threading.Tasks;

/// <summary>
/// 合成器的最小统一接口。不同 provider 的鉴权、请求格式、响应解析只在 backend 内处理。
/// </summary>
public interface ISpeechSynthesizerBackend : IDisposable
{
    void SetVoiceGender(SpeechSynthesizerController.SpeechVoiceGender gender);
    Task SpeakText(SpeechManager host, string text, Action onSpeakComplete);
    Task StopSpeaking();
}
