using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class SpeechSynthesizerBackendUtils
{
    public static IEnumerator SendNetworkRequest(BaseRequestParam requestParam, Action<bool, NetworkResponse> onComplete)
    {
        bool isCompleted = false;
        bool success = false;
        NetworkResponse response = null;

        ManagerRefer.NetworkServiceManager.SendRequest(requestParam, false, null, (result, networkResponse) =>
        {
            success = result;
            response = networkResponse;
            isCompleted = true;
        });

        while (!isCompleted)
        {
            yield return null;
        }

        onComplete?.Invoke(success, response);
    }

    public static IEnumerator PlayClip(SpeechManager host, AudioClip clip, Action onSpeakComplete, TaskCompletionSource<bool> tcs, string providerName)
    {
        if (clip == null)
        {
            Debug.LogError($"[SpeechSynthesizerController] {providerName} audio could not be decoded.");
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        var audioSource = host.SpeechAudioSource;
        if (audioSource == null)
        {
            Debug.LogError("[SpeechSynthesizerController] SpeechManager has no AudioSource.");
            host.FinishExternalSpeech(null);
            tcs.TrySetResult(false);
            yield break;
        }

        // 非 Azure provider 统一走本地 AudioSource；播放完成后由 SpeechManager 清理字幕和口型状态。
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
        Debug.Log($"[SpeechSynthesizerController] {providerName} audio playing. length={clip.length:F2}s, samples={clip.samples}, channels={clip.channels}, frequency={clip.frequency}, volume={audioSource.volume}, enabled={audioSource.enabled}");

        while (!host.IsSpeechShuttingDown && audioSource != null && audioSource.isPlaying)
        {
            yield return null;
        }

        host.FinishExternalSpeech(onSpeakComplete);
        tcs.TrySetResult(true);
    }

    public static AudioClip TryCreateWavAudioClip(byte[] wavData, string clipName)
    {
        // MimoTTS 当前按 wav/pcm16 返回，手动解析 RIFF，避免依赖临时文件。
        if (wavData == null || wavData.Length < 44 ||
            Encoding.ASCII.GetString(wavData, 0, 4) != "RIFF" ||
            Encoding.ASCII.GetString(wavData, 8, 4) != "WAVE")
        {
            return null;
        }

        int offset = 12;
        ushort audioFormat = 1;
        ushort channels = 1;
        int sampleRate = 24000;
        ushort bitsPerSample = 16;
        int dataOffset = -1;
        int dataSize = 0;

        while (offset + 8 <= wavData.Length)
        {
            string chunkId = Encoding.ASCII.GetString(wavData, offset, 4);
            int chunkSize = BitConverter.ToInt32(wavData, offset + 4);
            int chunkDataOffset = offset + 8;
            if (chunkSize < 0 || chunkDataOffset > wavData.Length)
            {
                return null;
            }

            int availableChunkSize = Math.Min(chunkSize, wavData.Length - chunkDataOffset);

            if (chunkId == "fmt " && availableChunkSize >= 16)
            {
                audioFormat = BitConverter.ToUInt16(wavData, chunkDataOffset);
                channels = BitConverter.ToUInt16(wavData, chunkDataOffset + 2);
                sampleRate = BitConverter.ToInt32(wavData, chunkDataOffset + 4);
                bitsPerSample = BitConverter.ToUInt16(wavData, chunkDataOffset + 14);
            }
            else if (chunkId == "data")
            {
                dataOffset = chunkDataOffset;
                dataSize = availableChunkSize;
                break;
            }

            offset = chunkDataOffset + chunkSize + (chunkSize % 2);
        }

        if (dataOffset < 0 || channels == 0 || sampleRate <= 0 || dataSize <= 0)
        {
            return null;
        }

        int bytesPerSample = bitsPerSample / 8;
        if (bytesPerSample <= 0)
        {
            return null;
        }

        int totalSampleCount = dataSize / bytesPerSample;
        totalSampleCount -= totalSampleCount % channels;
        if (totalSampleCount <= 0 || dataOffset + totalSampleCount * bytesPerSample > wavData.Length)
        {
            return null;
        }

        float[] samples = new float[totalSampleCount];
        for (int i = 0; i < totalSampleCount; i++)
        {
            int sampleOffset = dataOffset + i * bytesPerSample;
            if (!TryReadWavSample(wavData, sampleOffset, audioFormat, bitsPerSample, out samples[i]))
            {
                return null;
            }
        }

        var clip = AudioClip.Create(clipName, totalSampleCount / channels, channels, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static bool TryReadWavSample(byte[] wavData, int offset, ushort audioFormat, ushort bitsPerSample, out float sample)
    {
        sample = 0f;
        if (offset < 0 || offset >= wavData.Length)
        {
            return false;
        }

        if (audioFormat == 3 && bitsPerSample == 32)
        {
            if (offset + 4 > wavData.Length)
            {
                return false;
            }

            sample = Mathf.Clamp(BitConverter.ToSingle(wavData, offset), -1f, 1f);
            return true;
        }

        if (audioFormat != 1)
        {
            return false;
        }

        switch (bitsPerSample)
        {
            case 8:
                sample = (wavData[offset] - 128) / 128f;
                return true;
            case 16:
                if (offset + 2 > wavData.Length)
                {
                    return false;
                }

                sample = BitConverter.ToInt16(wavData, offset) / 32768f;
                return true;
            case 24:
                if (offset + 3 > wavData.Length)
                {
                    return false;
                }

                int value24 = wavData[offset] | (wavData[offset + 1] << 8) | (wavData[offset + 2] << 16);
                if ((value24 & 0x800000) != 0)
                {
                    value24 |= unchecked((int)0xFF000000);
                }

                sample = value24 / 8388608f;
                return true;
            case 32:
                if (offset + 4 > wavData.Length)
                {
                    return false;
                }

                sample = BitConverter.ToInt32(wavData, offset) / 2147483648f;
                return true;
            default:
                return false;
        }
    }
}
