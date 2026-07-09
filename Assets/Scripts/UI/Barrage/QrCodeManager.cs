using System;
using UnityEngine;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

/// <summary>
/// 本地二维码贴图生成管理器。
/// 
/// 弹幕系统需要在 Unity 画面上显示一个可扫描的二维码，二维码内容通常是手机弹幕网页 URL。
/// 这里把 ZXing.Net 的二维码生成能力封装在 Manager 里，业务组件只需要传入字符串并拿到 Texture2D。
/// 
/// 这样做的好处：
/// 1. <see cref="InteractiveBarrageClient"/> 不需要直接依赖 ZXing 的具体 API。
/// 2. 后续如果换二维码库，只需要改这个 Manager。
/// 3. 生成结果是 Unity 原生 Texture2D，可以直接赋给 RawImage.texture。
/// </summary>
public class QrCodeManager : BaseManager
{
    /// <summary>
    /// 根据文本内容生成二维码贴图。
    /// </summary>
    /// <param name="content">
    /// 二维码写入内容。当前弹幕场景中通常是手机网页地址，例如 http://server:37621/s/default。
    /// </param>
    /// <param name="pixelsPerModule">
    /// 每个二维码模块对应的像素数。数值越大贴图越清晰，但贴图尺寸也更大。
    /// </param>
    /// <param name="margin">
    /// 二维码四周留白模块数。留白太小会降低部分手机扫码成功率。
    /// </param>
    /// <returns>
    /// 成功时返回可直接赋给 RawImage.texture 的 Texture2D；失败或内容为空时返回 null。
    /// </returns>
    public UnityEngine.Texture2D GenerateTexture(string content, int pixelsPerModule = 8, int margin = 4)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        // 防止外部传入 0 或负数导致 ZXing 生成异常。
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
            // ZXing 输出 RGBA 像素数据，Unity Texture2D 可以直接 LoadRawTextureData。
            PixelData pixelData = writer.Write(content);
            UnityEngine.Texture2D texture = new UnityEngine.Texture2D(pixelData.Width, pixelData.Height, UnityEngine.TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(pixelData.Pixels);
            texture.Apply(false, false);
            texture.name = "BarrageQrCode";

            // 二维码需要边缘清晰，使用 Point 过滤避免缩放时变模糊。
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
