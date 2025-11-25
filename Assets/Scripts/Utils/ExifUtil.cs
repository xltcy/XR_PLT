using ExifLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ExifUtil
{
    public static ExifOrientation ReadExifOrientation(byte[] jpegBytes)
    {
        using (var ms = new System.IO.MemoryStream(jpegBytes))
        {
            // ExifLib 读取示例
            var reader = new ExifReader(ms);
            var res =  reader.GetTagValue(ExifTags.Orientation, out ushort ori)
                   ? ori : (ushort)1;
            switch(res)
            {
                case 3: return ExifOrientation.ROTATE180;
                case 6: return ExifOrientation.ROTATE90; // 顺时针 90°
                case 8: return ExifOrientation.ROTATE270;  // 逆时针 90°
                default: return ExifOrientation.NORMAL;
            }
        }
    }

    public static Texture2D FixOrientation(byte[] jpegBytes)
    {
        var ori = ExifOrientation.NORMAL;
        try
        {
            ori = ReadExifOrientation(jpegBytes);
        }
        catch(Exception e)
        {
            Debug.Log($"Fail to read exif: {e.Message}");
        }
        var texture = new Texture2D(2, 2);
        texture.LoadImage(jpegBytes);
        return FixOrientation(texture, ori);
    }

    // 根据 Orientation 把 Texture2D 旋转到正确方向
    public static Texture2D FixOrientation(Texture2D tex, ExifOrientation ori)
    {
        switch (ori)
        {
            case ExifOrientation.ROTATE180: return Texture2DRotateUtil.Rotate180(tex);
            case ExifOrientation.ROTATE270: return Texture2DRotateUtil.Rotate270(tex); // 顺时针 90°
            case ExifOrientation.ROTATE90: return Texture2DRotateUtil.Rotate90(tex);  // 逆时针 90°
            default: return tex;                                // 1 = 正常
        }
    }


    /**
     * clockwise rotate
     */
    public enum ExifOrientation
    {
        NORMAL,//1
        ROTATE90,//6
        ROTATE180,//3
        ROTATE270//8
    }
}
