using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Texture2DRotateUtil
{
    // 顺时针旋转90°
    public static Texture2D Rotate90(Texture2D src)
    {
        int width = src.width;
        int height = src.height;
        Texture2D rotated = new Texture2D(height, width, src.format, false);

        Color[] srcPixels = src.GetPixels();
        Color[] rotatedPixels = new Color[srcPixels.Length];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                rotatedPixels[y + (width - 1 - x) * height] = srcPixels[x + y * width];
            }
        }

        rotated.SetPixels(rotatedPixels);
        rotated.Apply();
        return rotated;
    }

    // 顺时针旋转180°
    public static Texture2D Rotate180(Texture2D src)
    {
        int width = src.width;
        int height = src.height;
        Texture2D rotated = new Texture2D(width, height, src.format, false);

        Color[] srcPixels = src.GetPixels();
        Color[] rotatedPixels = new Color[srcPixels.Length];

        for (int i = 0; i < srcPixels.Length; i++)
        {
            rotatedPixels[i] = srcPixels[srcPixels.Length - 1 - i];
        }

        rotated.SetPixels(rotatedPixels);
        rotated.Apply();
        return rotated;
    }

    // 顺时针旋转270°
    public static Texture2D Rotate270(Texture2D src)
    {
        int width = src.width;
        int height = src.height;
        Texture2D rotated = new Texture2D(height, width, src.format, false);

        Color[] srcPixels = src.GetPixels();
        Color[] rotatedPixels = new Color[srcPixels.Length];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                rotatedPixels[(height - 1 - y) + x * height] = srcPixels[x + y * width];
            }
        }

        rotated.SetPixels(rotatedPixels);
        rotated.Apply();
        return rotated;
    }

    public static Texture2D RotateByOrientation(Texture2D src)
    {
        switch (Input.deviceOrientation)
        {
            case DeviceOrientation.Portrait:
                return Rotate90(src);

            case DeviceOrientation.PortraitUpsideDown:
                return Rotate270(src);

            case DeviceOrientation.LandscapeLeft:
                return src;

            case DeviceOrientation.LandscapeRight:
                return Rotate180(src);

            default:
                // 如果无法判断，保守使用 Rotate90（iPhone 默认 portrait）
                return Rotate90(src);
        }
    }
}
