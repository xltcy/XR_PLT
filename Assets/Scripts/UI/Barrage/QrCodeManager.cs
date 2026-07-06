using System;
using UnityEngine;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

/// <summary>
/// 本地二维码贴图生成管理器。
/// 这里把第三方库 ZXing.Net 包在 Manager 内，避免业务脚本直接依赖二维码库的具体 API。
/// </summary>
public class QrCodeManager : BaseManager
{
    /// <summary>
    /// 根据文本内容生成可直接赋给 RawImage.texture 的二维码贴图。
    /// </summary>
    /// <param name="content">二维码内写入的内容，当前用于写入手机弹幕网页 URL。</param>
    /// <param name="pixelsPerModule">每个二维码模块对应的像素数，数值越大贴图越清晰。</param>
    /// <param name="margin">二维码四周留白模块数，留白太小可能影响手机扫码。</param>
    public UnityEngine.Texture2D GenerateTexture(string content, int pixelsPerModule = 8, int margin = 4)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        int modulePixels = Mathf.Max(1, pixelsPerModule);
        int quietZone = Mathf.Max(0, margin);

        BarcodeWriterPixelData writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Margin = quietZone,
                Width = 33 * modulePixels,
                Height = 33 * modulePixels
            }
        };

        try
        {
            PixelData pixelData = writer.Write(content);
            UnityEngine.Texture2D texture = new UnityEngine.Texture2D(pixelData.Width, pixelData.Height, UnityEngine.TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(pixelData.Pixels);
            texture.Apply(false, false);
            texture.name = "BarrageQrCode";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            return texture;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[QrCodeManager] Generate QR code failed: {e.Message}");
            return null;
        }
    }
}
