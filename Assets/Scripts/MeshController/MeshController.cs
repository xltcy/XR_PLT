using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Threading;
using UniGLTF;
using UnityEngine.EventSystems;

public class MeshController : BaseController
{
    #region 属性
    public Camera arCamera;

    public GameObject ModelInstance
    {
        private set => ModelDict[nameof(RelocateController.RelocateType.Scene)] = value;
        get
        {
            ModelDict.TryGetValue(nameof(RelocateController.RelocateType.Scene), out var go);
            return go;
        }
    }

    private GameObject sonarGO
    {
        set => ModelDict[nameof(RelocateController.RelocateType.Sonar)] = value;
        get
        {
            ModelDict.TryGetValue(nameof(RelocateController.RelocateType.Sonar), out var go);
            return go;
        }
    }
    
    private Dictionary<string, GameObject> ModelDict = new Dictionary<string, GameObject>();
    
    private Shader defaultShader;
    private Shader hideShader;
    #endregion

    #region 生命周期
    public override void OnRegister()
    {
        base.OnRegister();
        InitShader();
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
    }
    
    void InitShader()
    {
        defaultShader = Shader.Find("Universal Render Pipeline/Lit");
        hideShader = Shader.Find("VR/SpatialMapping/Occlusion");
    }
    #endregion

    #region 相机和图片数据

    /// <summary>
    /// 获取相机位姿
    /// </summary>
    /// <returns></returns>
    public Matrix4x4 GetARCameraPose()
    {
        if (arCamera == null)
        {
            Debug.LogError("找不到AR相机");
            return default(Matrix4x4);
        }
        Vector3 camPosition = arCamera.transform.position;
        Quaternion camRotation = arCamera.transform.rotation;
        return Matrix4x4.TRS(camPosition, camRotation, Vector3.one);
    }

    /// <summary>
    /// 获取相机图片RawData
    /// </summary>
    /// <returns>byte[]</returns>
    public byte[] GetCameraImgRawData()
    {
        byte[] rawData = new byte[] { };
        if (Application.platform == RuntimePlatform.Android)
        {
            // load from camera android aar
            rawData = GetImageByARFoundation();
        }

        return rawData;
    }

    /// <summary>
    /// 获取本地图片RawData
    /// </summary>
    /// <returns>byte[]</returns>
    public byte[] GetLocalImgRawData(string imgPath)
    {
        byte[] rawData = new byte[] { };
        if (!imgPath.IsNullOrEmpty())
        {
            Debug.Log($"读取图片rawdata：{imgPath}");
            rawData = ReadImageBytes(imgPath);
        }

        return rawData;
    }
    
    byte[] ReadImageBytes(string path)
    {
        try
        {
            // 使用 File.ReadAllBytes 读取本地图片的字节数组
            byte[] fileData = File.ReadAllBytes(path);
            var texture = ExifUtil.FixOrientation(fileData);
            return texture.EncodeToPNG();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read image bytes: {e.Message}\n\nstack: {e.StackTrace}");
            return null;
        }
    }

    private byte[] GetImageByARFoundation()
    {
        arCamera.GetComponent<ARCameraManager>().TryAcquireLatestCpuImage(out XRCpuImage image);

        // mirror vertically not horizental.
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorX
        };

        //// Get native data.
        var renderTexture = new Texture2D(image.width, image.height, conversionParams.outputFormat, false);
        int dataSize = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(dataSize, Allocator.Temp);
        image.Convert(conversionParams, buffer);
        image.Dispose();
        renderTexture.LoadRawTextureData(buffer);
        renderTexture.Apply();
        buffer.Dispose();

        // fix image orientation.
        renderTexture = Texture2DRotateUtil.RotateByOrientation(renderTexture);
        return renderTexture.EncodeToJPG();
    }
    #endregion

    #region 放置模型
    public void ClickToSummonAtCamera()
    {
        ClickToSummonAtCamera(ControllerRefer.RelocateController.GetPoseByEnumType(RelocateController.RelocateType.Scene));
    }
    
    public void ClickToSummonAtCamera(Pose pose)
    {
        // init modelInstance
        var go = ControllerRefer.SceneController.AnalysisSceneData();
        ModelInstance = go;
        // set AstarPath ConsPos;
        Vector3 centerPos = AstarPath.active.data.recastGraph.forcedBoundsCenter;
        SMPLController.SetConsPos(centerPos);
        
        go.transform.position = pose.position * GetModelScale(go);
        go.transform.rotation = pose.rotation;
    }

    public void ClickToSummonSonarAtCamera(Pose pose)
    {
        ManagerRefer.GameObjectPoolManager.Recycle(sonarGO);
        ManagerRefer.GameObjectPoolManager.InstantiateAsync("Prefab/Prefab-Sonar", null, go =>
        {
            if (go == null) return;
            sonarGO = go;
            
            float scale = GetModelScale(go);
            //绕z轴旋转180
            Quaternion rot180 = Quaternion.AngleAxis(180f, pose.rotation * Vector3.forward);

            // 更新 Pose 的旋转
            pose.rotation = rot180 * pose.rotation;

            go.transform.position = pose.position * scale;
            go.transform.rotation = pose.rotation;
        
        
            //todo 删除临时代码
            Vector3 tempPos = new Vector3(-7.065f, -0.135f, 2.737f); //相对场景的坐标
            go.transform.position = ControllerRefer.SceneController.Scene.transform.TransformPoint(tempPos);
            Quaternion rotationOffset = new Quaternion(0.0f, 0.156820267f, 0.0f, 0.987627208f); //相对场景的旋转
            go.transform.rotation = ControllerRefer.SceneController.Scene.transform.rotation * rotationOffset;
        
            go.SetVisible(true);
        });
    }

    public Pose TransArrayToWorldPose(Matrix4x4 camPose, float[,] num)
    {
        Matrix4x4 c2w = MatrixUtil.FloatArrayToMatrix(num);
        MatrixUtil.PrintMatrix(c2w, "后端返回的原始Pose");
        Matrix4x4 w2c = camPose * c2w.inverse;
        // 目前假设模型是3D Scanner重建的(RUB)，且是obj文件(is_wavefront)
        // TODO 把模型的坐标系和是否为obj文件写入到场景的json中
        Matrix4x4 coord_xform = MatrixUtil.GetCoordXform("RUB", is_wavefront: true);
        MatrixUtil.PrintMatrix(w2c * coord_xform, "放置模型时使用的Pose");
        return MatrixUtil.MatrixToPose(w2c * coord_xform);
    }
    #endregion
    
    #region 模型信息
    public static float GetModelScale(GameObject go)
    {
        Transform mesh = go.transform.GetChildByName("mesh");
        if (mesh != null)
        {
            return mesh.localScale.x;
        }

        for (int i = 0; i < go.transform.childCount; i++)
        {
            var childTrans = go.transform.GetChild(i);
            if (childTrans.name.ToLower().Contains("FindPath".ToLower()))
            {
                continue;
            }
            return childTrans.localScale.x;
        }
        
        return go.transform.localScale.x;
    }
    #endregion

    #region 模型旋转
    public void ClickRotateR()
    {
        ModelInstance.transform.RotateAround(ModelInstance.transform.position, ModelInstance.transform.right, 90f);
    }

    public void ClickRotateF()
    {
        ModelInstance.transform.RotateAround(ModelInstance.transform.position, ModelInstance.transform.forward, 90f);
    }

    public void ClickRotateU()
    {
        ModelInstance.transform.RotateAround(ModelInstance.transform.position, ModelInstance.transform.up, 90f);
    }
    #endregion

    #region 模型透明控制
    public void HideMeshRender()
    {
        ChangeMeshShaderWithTag("Mesh", hideShader);
    }

    public void ShowMeshRender()
    {
        ChangeMeshShaderWithTag("Mesh", defaultShader);
    }

    public void HideSonarRender()
    {
        ChangeMeshShaderWithTag("Sonar", hideShader);
    }

    public void ShowSonarRender()
    {
        ChangeMeshShaderWithTag("Sonar", defaultShader);
    }
    
    //todo 不使用Tag控制
    public void ChangeMeshShaderWithTag(string tag, Shader targetShader)
    {
        GameObject obj = GameObject.FindGameObjectWithTag(tag);
        if (obj == null)
        {
            return;
        }
        
        MeshRenderer[] meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach(MeshRenderer meshRenderer in meshRenderers)
        {
            Material[] materials = meshRenderer.materials;
            foreach(Material material in materials)
            {
                material.shader = targetShader;
            }
        }
    }

    #endregion

}
