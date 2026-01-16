using System.Threading;
using UnityEngine;
using Network.RequestParam;

public class RelocateController : BaseController
{
    private MeshController meshController;
    
    public override void OnRegister()
    {
        base.OnRegister();
        
        meshController = ControllerRefer.MeshController;
        
        ManagerRefer.NetworkServiceManager.AddResponseListener(NetworkConstant.RELOCATE_SCENE, OnRelocateSceneResponse);
    }

    public void RelocateSceneRequest(byte[] rawData, CountdownEvent countdown = null)
    {
        var summaryItemData = meshController.GetCurrentSummaryItemData();
        if (summaryItemData == null)
        {
            Utils.LogMessage(LogType.Error, true, $"RelocateSceneRequest失败，当前没有选中的场景数据");
            return;
        }
        
        // Record Camera Pose
        var arCamera = meshController.arCamera;
        var camPose = Matrix4x4.TRS(arCamera.transform.position, arCamera.transform.rotation, Vector3.one);
        
        var req = new RelocateScene.RequestParam(summaryItemData, rawData);
        req.localData = new RelocateScene.LocalData {camPose = camPose, countdown = countdown}; 
        req.Send();
    }

    public void OnRelocateSceneResponse(bool result, NetworkResponse response)
    {
        if (result)
        {
            var data = response.GetData<RelocateScene.ResponseData>();
            Debug.Log(data.message);
            
            // 获取相机位姿
            var localData = response.localData as RelocateScene.LocalData;
            var camPose = localData?.camPose ?? default;

            // 手动解析 pose 字段
            data.ParsePoseFromJson(response.rawResponse);

            // 转 Matrix4x4
            //Matrix4x4 matrix = data.ToMatrix();

            // 转 Pose
            meshController.relocatedPose =  meshController.TransArrayToWorldPose(camPose, data.poseMatrix);

            // 输出测试
            Debug.Log($"Position: {meshController.relocatedPose.position}, Rotation: {meshController.relocatedPose.rotation}");
            
            // 通知等待的线程
            if (localData != null && localData.countdown != null)
            {
                localData.countdown.Signal();
            }
        }
    }
}
