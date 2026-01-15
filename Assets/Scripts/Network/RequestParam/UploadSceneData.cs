using System.Collections.Generic;
using UnityEngine.Networking;

namespace Network.RequestParam
{
    /// <summary>
    /// 上传场景数据，以更新服务器文件
    /// </summary>
    public static class UploadSceneData
    {
        public class RequestParam : BaseRequestParam
        {
            public RequestParam(string sceneKey, string json)
            {
                url = ManagerRefer.NetworkServiceManager.BuildUrl($"{NetworkUtil.UPLOAD_SCENE_CONFIG_INTERFACE}?key={UnityWebRequest.EscapeURL(sceneKey)}");
                method = "POST";

                networkConstant = NetworkConstant.UPLOAD_SCENE_DATA;
        
                FormDataFields = new List<FormField> { FormField.CreateText("config", json) };
            }
        }
    }
}