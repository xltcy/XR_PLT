using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace Network.RequestParam
{
    /// <summary>
    /// 场景重定位
    /// </summary>
    public static class RelocateScene
    {
        public class RequestParam : BaseRequestParam
        {
            public RequestParam(SummaryItemData summaryItemData, byte[] imageData)
            {
                var endpoint = NetworkUtil.GetRelocateUrlSuffix(summaryItemData);
                queryParams.Add("source_location", summaryItemData.sceneDataSet);
                url = ManagerRefer.NetworkServiceManager.BuildUrl(endpoint);
                method = "POST";
                networkConstant = NetworkConstant.RELOCATE_SCENE;
        
                // 使用自定义 boundary
                string timestamp = "---------------------" + System.DateTime.Now.Ticks.ToString("x");
                customBoundary = timestamp;
        
                // 添加图片文件
                FormDataFields = new List<FormField> { FormField.CreateFile("images", imageData, "image.jpg", "image/jpg") };
            }
        }

        public class LocalData
        {
            public Matrix4x4 camPose;
            public CountdownEvent countdown;
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
                // 假设 receivedJson 是接收到的 JSON 字符串
                int startIndex = json.IndexOf("[[");
                int endIndex = json.IndexOf("]]");
                string truncatedJson = json.Substring(startIndex + 1, endIndex - startIndex + 1);
                
                // 用正则匹配所有数字
                string pattern = @"[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?";
                var matches = Regex.Matches(truncatedJson, pattern);

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
