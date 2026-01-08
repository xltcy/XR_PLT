using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Globalization;

public class MatrixUtil
{
    #region 矩阵读取
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
    #endregion

    #region 数据结构转换
    /// <summary>
    /// 将矩阵转换为Transform的position和rotation
    /// </summary>
    public static Pose MatrixToPose(Matrix4x4 matrix)
    {
        return new Pose(matrix.GetColumn(3), matrix.rotation);
    }

    public static Pose FloatArrayToPose(float[,] floatArray)
    {
        return MatrixToPose(FloatArrayToMatrix(floatArray));
    }
    
    public static Matrix4x4 PoseToMatrix(Pose pose)
    {
        return Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
    }

    public static Matrix4x4 FloatArrayToMatrix(float[,] floatArray)
    {
        // 获取数组维度
        int rows = floatArray.GetLength(0);
        int cols = floatArray.GetLength(1);

        // 检查数组维度
        if ((rows != 3 && rows != 4) || cols != 4)
        {
            throw new ArgumentException($"数组必须是3x4或4x4的，当前形状是[{rows},{cols}]");
        }

        // 创建并返回Matrix4x4
        return new Matrix4x4(
            new Vector4(floatArray[0, 0], floatArray[1, 0], floatArray[2, 0], 0),
            new Vector4(floatArray[0, 1], floatArray[1, 1], floatArray[2, 1], 0),
            new Vector4(floatArray[0, 2], floatArray[1, 2], floatArray[2, 2], 0),
            new Vector4(floatArray[0, 3], floatArray[1, 3], floatArray[2, 3], 1)
        );
    }
    #endregion

    #region 坐标系变换
    private static Dictionary<char, Vector3> coordAxisMap = new Dictionary<char, Vector3>()
    {
        {'R', new Vector4(1, 0, 0, 0)},
        {'L', new Vector4(-1, 0, 0, 0)},
        {'U', new Vector4(0, 1, 0, 0)},
        {'D', new Vector4(0, -1, 0, 0)},
        {'F', new Vector4(0, 0, 1, 0)},
        {'B', new Vector4(0, 0, -1, 0)},
        {'r', new Vector4(1, 0, 0, 0)},
        {'l', new Vector4(-1, 0, 0, 0)},
        {'u', new Vector4(0, 1, 0, 0)},
        {'d', new Vector4(0, -1, 0, 0)},
        {'f', new Vector4(0, 0, 1, 0)},
        {'b', new Vector4(0, 0, -1, 0)}
    };

    public static Matrix4x4 GetCoordXform(string src_coord, string dst_coord="RUF", bool is_wavefront=true)
    {
        Matrix4x4 coord_xform = Matrix4x4.identity;

        Matrix4x4 src_base = new Matrix4x4(
            coordAxisMap[src_coord[0]],
            coordAxisMap[src_coord[1]],
            coordAxisMap[src_coord[2]],
            new Vector4(0, 0, 0, 1)
        );
        Matrix4x4 dst_base = new Matrix4x4(
            coordAxisMap[dst_coord[0]],
            coordAxisMap[dst_coord[1]],
            coordAxisMap[dst_coord[2]],
            new Vector4(0, 0, 0, 1)
        );

        coord_xform = dst_base.transpose * src_base;

        // obj文件则翻转X轴
        if (!is_wavefront) return coord_xform;

        Matrix4x4 x_axis_flip =new Matrix4x4(
            new Vector4(-1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(0, 0, 0, 1)
        );
        return coord_xform * x_axis_flip;
    }
    #endregion

    #region Debug
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
    #endregion
}
