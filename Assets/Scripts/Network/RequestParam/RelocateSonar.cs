using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Network.RequestParam
{
    /// <summary>
    /// 声呐重定位
    /// </summary>
    
    public static class RelocateSonar
    {
        public class RequestParam : BaseRequestParam
        {
            public RequestParam(string name, byte[] imageData)
            {
                url = ManagerRefer.NetworkServiceManager.BuildUrl($"/media_app/obj_pose_estimate");
                queryParams.Add("obj_name", name);
                method = "POST";
                networkConstant = NetworkConstant.RELOCATE_SONAR;

                // 添加图片文件
                FormDataFields = new List<FormField> { FormField.CreateFile("image", imageData, "getPoseImage.png") };
            }
        }
        
        public class ResponseData
        {
            public string message;
            public float[,] poseMatrix; // 存 3x4 矩阵

            /// <summary>
            /// 手动解析 pose JSON 字符串，填充 poseMatrix
            /// </summary>
            public void ParsePoseFromJson(string json)
            {
                // 用正则匹配所有数字
                string pattern = @"[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?";
                var matches = Regex.Matches(json, pattern);

                if (matches.Count != 12)
                {
                    Debug.LogError("Pose json format incorrect! Need 12 numbers (3x4).");
                    poseMatrix = new float[3, 4];
                    return;
                }

                poseMatrix = new float[3, 4];

                for (int i = 0; i < 12; i++)
                {
                    poseMatrix[i / 4, i % 4] = float.Parse(matches[i].Value);
                }
            }

            /// <summary>
            /// 转成 Unity Matrix4x4
            /// </summary>
            public Matrix4x4 ToMatrix()
            {
                return MatrixUtil.FloatArrayToMatrix(poseMatrix);
            }

            /// <summary>
            /// 转成 Unity Pose
            /// </summary>
            public Pose ToPose()
            {
                return MatrixUtil.FloatArrayToPose(poseMatrix);
            }
        }
    }
}
