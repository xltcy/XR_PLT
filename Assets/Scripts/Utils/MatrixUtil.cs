using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using static UnityEngine.GraphicsBuffer;
using UnityEditor;

public class MatrixUtil
{
    /// <summary>
    /// 从文件读取Matrix4x4
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>UnityEngine.Matrix4x4对象</returns>
    public static Matrix4x4 ReadMatrixFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"文件未找到: {filePath}");
            return Matrix4x4.identity;
        }

        try
        {
            string content = File.ReadAllText(filePath);
            return ParseMatrix(content);
        }
        catch (Exception e)
        {
            Debug.LogError($"读取文件失败: {e.Message}");
            return Matrix4x4.identity;
        }
    }

    /// <summary>
    /// 解析矩阵字符串为Matrix4x4
    /// </summary>
    /// <param name="matrixString">包含16个数值的字符串</param>
    /// <returns>Matrix4x4对象</returns>
    public static Matrix4x4 ParseMatrix(string matrixString)
    {
        // 清理字符串，分割为数值数组
        string[] stringValues = matrixString
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Replace('\t', ' ')
            .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (stringValues.Length != 16)
        {
            Debug.LogError($"需要16个数值，但找到了 {stringValues.Length} 个");
            return Matrix4x4.identity;
        }

        // 解析为float数组
        float[] values = new float[16];
        for (int i = 0; i < 16; i++)
        {
            values[i] = ParseFloat(stringValues[i]);
        }

        return new Matrix4x4
        {
            m00 = values[0],
            m01 = values[1],
            m02 = values[2],
            m03 = values[3],
            m10 = values[4],
            m11 = values[5],
            m12 = values[6],
            m13 = values[7],
            m20 = values[8],
            m21 = values[9],
            m22 = values[10],
            m23 = values[11],
            m30 = values[12],
            m31 = values[13],
            m32 = values[14],
            m33 = values[15]
        };
    }

    /// <summary>
    /// 解析科学计数法的浮点数
    /// </summary>
    private static float ParseFloat(string value)
    {
        try
        {
            // 使用不变区域性确保正确解析科学计数法
            return float.Parse(value, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            Debug.LogError($"无法解析数值: {value}");
            return 0f;
        }
    }

    /// <summary>
    /// 打印矩阵信息
    /// </summary>
    public static void PrintMatrix(Matrix4x4 matrix, string name = "Matrix")
    {
        Debug.Log($"{name}:\n" +
            $"[{matrix.m00:F8}, {matrix.m01:F8}, {matrix.m02:F8}, {matrix.m03:F8}]\n" +
            $"[{matrix.m10:F8}, {matrix.m11:F8}, {matrix.m12:F8}, {matrix.m13:F8}]\n" +
            $"[{matrix.m20:F8}, {matrix.m21:F8}, {matrix.m22:F8}, {matrix.m23:F8}]\n" +
            $"[{matrix.m30:F8}, {matrix.m31:F8}, {matrix.m32:F8}, {matrix.m33:F8}]");
    }

    /// <summary>
    /// 将矩阵转换为Transform的position和rotation
    /// </summary>
    public static Pose GetPose(Matrix4x4 matrix)
    {
        return new Pose(matrix.GetColumn(3), matrix.rotation);
    }
}
