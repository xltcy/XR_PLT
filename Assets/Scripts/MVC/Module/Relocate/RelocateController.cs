using System.Collections.Generic;
using UnityEngine;
using Network.RequestParam;

public class RelocateController : BaseController
{
    public enum RelocateType
    {
        Scene = 0,
        Sonar = 1
    }
    
    private MeshController meshController;
    private SceneController sceneController;
    
    private Dictionary<string, Pose> relocatePoses = new Dictionary<string, Pose>();
    
    #region 生命周期
    public override void OnRegister()
    {
        base.OnRegister();
        
        meshController = ControllerRefer.MeshController;
        sceneController = ControllerRefer.SceneController;
        
        relocatePoses.Clear();
    
        ManagerRefer.NetworkServiceManager.AddResponseListener(NetworkConstant.RELOCATE_SCENE, OnRelocateSceneResponse);
        ManagerRefer.NetworkServiceManager.AddResponseListener(NetworkConstant.RELOCATE_SONAR, OnRelocateSonarResponse);
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
        
        ManagerRefer.NetworkServiceManager.RemoveResponseListener(NetworkConstant.RELOCATE_SCENE, OnRelocateSceneResponse);
        ManagerRefer.NetworkServiceManager.RemoveResponseListener(NetworkConstant.RELOCATE_SONAR, OnRelocateSonarResponse);

    }
    #endregion 生命周期
    
    #region 公共方法
    public Pose GetPoseByStringType(string relocateType)
    {
        if (relocatePoses.IsNullOrEmpty() || !relocatePoses.TryGetValue(relocateType, out var pose))
        {
            return default(Pose);
        }
        return pose;
    }
    
    public void SetPoseByStringType(string relocateType, Pose pose)
    {
        relocatePoses[relocateType] = pose;
    }
    
    public Pose GetPoseByEnumType(RelocateType relocateType)
    {
        return GetPoseByStringType(relocateType.ToString());
    }
    
    public void SetPoseByEnumType(RelocateType relocateType, Pose pose)
    {
        SetPoseByStringType(relocateType.ToString(), pose);
    }
    #endregion 公共方法

    #region 网络请求
    /// <summary>
    /// 重定位场景
    /// </summary>
    /// <param name="rawData"></param>
    /// <param name="lockable"></param>
    /// <param name="fake"></param>
    public void RelocateSceneRequest(byte[] rawData, Transform lockable = null, bool fake = false)
    {
        if (fake)
        {
            this.TriggerEvent(EventConstant.COMPLETE_RELOCATE_SCENE);
            return;
        }
        
        var summaryItemData = sceneController.GetCurrentSummaryItemData();
        if (summaryItemData == null)
        {
            Utils.LogMessage(LogType.Error, true, $"RelocateSceneRequest失败，当前没有选中的场景数据");
            return;
        }
        
        // Record Camera Pose
        var arCamera = meshController.arCamera;
        var camPose = Matrix4x4.TRS(arCamera.transform.position, arCamera.transform.rotation, Vector3.one);
        
        var req = new RelocateScene.RequestParam(summaryItemData, rawData);
        req.localData = new RelocateScene.LocalData {camPose = camPose}; 
        req.Send(lockable);
    }

    /// <summary>
    /// 场景重定位响应
    /// </summary>
    /// <param name="result"></param>
    /// <param name="response"></param>
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
            var pose = meshController.TransArrayToWorldPose(camPose, data.poseMatrix);
            SetPoseByEnumType(RelocateType.Scene, pose);
            
            // 输出测试
            Debug.Log($"Position: {pose.position}, Rotation: {pose.rotation}");
            
            // 通知等待的线程
            if (localData != null && localData.countdown != null)
            {
                localData.countdown.Signal();
            }

            this.TriggerEvent(EventConstant.COMPLETE_RELOCATE_SCENE);
        }
    }
    
    /// <summary>
    /// 重定位声呐
    /// </summary>
    /// <param name="rawData"></param>
    /// <param name="fake">假请求，直接跳转定位完成的逻辑</param>
    public void RelocateSonarRequest(byte[] rawData, bool fake = false)
    {
        if (fake)
        {
            this.TriggerEvent(EventConstant.COMPLETE_RELOCATE_SONAR);
            return;
        }
        
        // Record Camera Pose
        var camPose = meshController.GetARCameraPose();
        var req = new Network.RequestParam.RelocateSonar.RequestParam("sonar", rawData)
        {
            localData = camPose
        };
        req.Send();
    }
    
    public void OnRelocateSonarResponse(bool result, NetworkResponse response)
    {
        if (result)
        {
            var data = response.GetData<Network.RequestParam.RelocateSonar.ResponseData>();
            Debug.Log(data.message);
            
            // 获取相机位姿
            var camPose = response.localData is Matrix4x4 ? (Matrix4x4)response.localData : default;

            // 手动解析 pose 字段
            data.ParsePoseFromJson(response.rawResponse);

            // 转 Pose
            var pose = meshController.TransArrayToWorldPose(camPose, data.poseMatrix); 
            SetPoseByEnumType(RelocateType.Sonar, pose);
            
            // 输出测试
            Debug.Log($"Position: {pose.position}, Rotation: {pose.rotation}");

            this.TriggerEvent(EventConstant.COMPLETE_RELOCATE_SONAR);
        }
    }
    #endregion 网络请求
}
