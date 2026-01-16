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

public class MeshController : BaseController
{
    public Camera arCamera;
    private GameObject modelInstance;
    private GameObject sonarGO;

    public Dropdown sceneSelectDropdown;

    public Pose relocatedPose;
    private Pose relocatedSonarPose;

    public Button buttonGetPose;
    public Button buttonSummonAtCamera;

    public Button buttonHideMesh;
    public Button buttonShowMesh;

    public Button buttonHideSonar;
    public Button buttonShowSonar;

    public TMP_InputField datasetLoc;

    [Header("本地图片路径")]
    public string testImagePath;

    private Matrix4x4 camPoseT0;

    private List<SummaryItemData> summary = new List<SummaryItemData>();
    private int selectedSceneIndex = 0;

    private Shader defaultShader;
    private Shader hideShader;

    enum StartState
    {
        Normal,
        GettingPos,
        WaitSummon,
        Summoning
    }

    public override void OnRegister()
    {
        base.OnRegister();
        
        sceneSelectDropdown.onValueChanged.AddListener(OnSceneSelectChanged);
        ManagerRefer.NetworkServiceManager.AddResponseListener(NetworkConstant.RELOCATE_SONAR, OnRelocateSonarResponse);

        Init();
    }

    public override void OnUnregister()
    {
        sceneSelectDropdown.onValueChanged.RemoveListener(OnSceneSelectChanged);
        ManagerRefer.NetworkServiceManager.RemoveResponseListener(NetworkConstant.RELOCATE_SONAR, OnRelocateSonarResponse);
        
        base.OnUnregister();
    }

    void Init()
    {
        //modelToSummon = (GameObject)Resources.Load("Prefab/Prefab-GXL"); // 在这里更换放置的模型
        //SetDropDownAddListener(模型切换);
        // 模型选择.value = 1;
        defaultShader = Shader.Find("Universal Render Pipeline/Lit");
        hideShader = Shader.Find("VR/SpatialMapping/Occlusion");
        buttonGetPose.gameObject.SetActive(true);
        buttonSummonAtCamera.gameObject.SetActive(false);
        
        sceneSelectDropdown.ClearOptions();

        SetStartState(StartState.Normal);

        buttonHideMesh.gameObject.SetActive(true);
        buttonShowMesh.gameObject.SetActive(false);

        defaultShader = Shader.Find("Universal Render Pipeline/Lit");
        hideShader = Shader.Find("VR/SpatialMapping/Occlusion");
    }
    
    public void InitSceneSummary(List<SummaryItemData> items)
    {
        summary = items;
        List<String> options = new List<string>();
        items.ForEach(item => options.Add(item.sceneName));
        sceneSelectDropdown.ClearOptions();
        sceneSelectDropdown.AddOptions(options);
        
        //手动初始化一次场景选择DropDown
        OnSceneSelectChanged(sceneSelectDropdown.value);
    }

    
    //设置模型选择
    private void SetDataSetLoc()
    {
        var curSummary = GetCurrentSummaryItemData();
        if (datasetLoc)
        {
            datasetLoc.text = curSummary?.sceneDataSet;
        }
    }


    public void HideMeshRender()
    {
        ChangeMeshShaderWithTag("Mesh", hideShader);

        buttonShowMesh.gameObject.SetActive(true);
        buttonHideMesh.gameObject.SetActive(false);
    }

    public void ShowMeshRender()
    {
        ChangeMeshShaderWithTag("Mesh", defaultShader);

        buttonShowMesh.gameObject.SetActive(false);
        buttonHideMesh.gameObject.SetActive(true);
    }

    public void HideSonarRender()
    {
        ChangeMeshShaderWithTag("Sonar", hideShader);

        buttonShowSonar.gameObject.SetActive(true);
        buttonHideSonar.gameObject.SetActive(false);
    }

    public void ShowSonarRender()
    {
        ChangeMeshShaderWithTag("Sonar", defaultShader);

        buttonShowSonar.gameObject.SetActive(false);
        buttonHideSonar.gameObject.SetActive(true);
    }

    #region 前后端通信
    public enum RelocateType
    {
        Scene = 0,
        Sonar = 1
    }

    public void ClickToGetPoseByCapture(RelocateType relocateType)
    {
        //relocateType == 0，重定位场景；relocateType == 1，重定位声呐。

        //DebugUIMediator节点上添加开关
        if (DebugSwitch.Instance.DEBUG_FAKE_RELOCATE && Application.isEditor)
        {
            tempGetPose();
            return;
        }

        SetStartState(StartState.GettingPos);
        UIStateManager.SetLoadingStatus(true);

        // Record Camera Pose
        Vector3 camPosition = arCamera.transform.position;
        Quaternion camRotation = arCamera.transform.rotation;
        camPoseT0 = Matrix4x4.TRS(camPosition, camRotation, Vector3.one);
        byte[] rawData;
        if (Application.platform == RuntimePlatform.Android)
        {
            // load from camera android aar
            rawData = GetImageByARFoundation();
        }
        else
        {
            //尝试读取DebugSwitch中的路径
            string debugImagePath = DebugSwitch.Instance.GetRelocateDebugImgPath(relocateType);
            if (!debugImagePath.IsNullOrEmpty())
            {
                Debug.Log($"重定位图片：{debugImagePath.ToString()}");
                rawData = ReadImageBytes(debugImagePath);
            }
            else
            {
                // load from file
                Debug.Log($"重定位图片：{testImagePath.ToString()}");
                rawData = ReadImageBytes(testImagePath);
            }
        }
        switch (relocateType)
        {
            case RelocateType.Scene:
                SendImageAndReadJson(rawData);
                break;
            case RelocateType.Sonar:
                SendSonarImage(rawData);
                break;
        }
    }

    private void SendImageAndReadJson(byte[] rawData)
    {
        CountdownEvent countdownEvent = new CountdownEvent(2);
        bool hasError = false;
        StartCoroutine(WaitUnitCountDownComplete(countdownEvent, () =>
        {
            if (hasError)
            {
                SetStartState(StartState.Normal);
            }
            else
            {
                SetStartState(StartState.WaitSummon);
            }
            UIStateManager.SetLoadingStatus(false);
        }));

        ControllerRefer.RelocateController.RelocateSceneRequest(rawData, countdownEvent);
        
        ControllerRefer.SceneController.RequestSceneDataByKey(summary[selectedSceneIndex],
            onComplete: (result, response) =>
            {
                hasError |= !result;
                countdownEvent.Signal();
                ControllerRefer.VoiceController.InitLLMMessageList();
            });
    }

    private void SendSonarImage(byte[] rawData)
    {
        // Record Camera Pose
        var camPose = Matrix4x4.TRS(arCamera.transform.position, arCamera.transform.rotation, Vector3.one);
        var req = new Network.RequestParam.RelocateSonar.RequestParam("sonar", rawData);
        req.localData = camPose; 
        req.Send();
    }

    private void OnRelocateSonarResponse(bool result, NetworkResponse response)
    {
        if (result)
        {
            var data = response.GetData<Network.RequestParam.RelocateSonar.ResponseData>();
            Debug.Log(data.message);
            
            // 获取相机位姿
            var camPose = response.localData is Matrix4x4 ? (Matrix4x4)response.localData : default;

            // 2️⃣ 手动解析 pose 字段
            data.ParsePoseFromJson(response.rawResponse);

            // 3️⃣ 转 Matrix4x4
            //Matrix4x4 matrix = data.ToMatrix();

            // 4️⃣ 转 Pose
            relocatedSonarPose = TransArrayToWorldPose(camPose, data.poseMatrix);

            // 5️⃣ 输出测试
            Debug.Log($"Position: {relocatedSonarPose.position}, Rotation: {relocatedSonarPose.rotation}");
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
        SetStartState(StartState.Summoning);

        // init modelInstance
        modelInstance = ControllerRefer.SceneController.AnalysisSceneData();
        // set AstarPath ConsPos;
        Vector3 centerPos = AstarPath.active.data.recastGraph.forcedBoundsCenter;
        SMPLController.SetConsPos(centerPos);
        
        modelInstance.transform.position = relocatedPose.position * GetModelScale(modelInstance);
        modelInstance.transform.rotation = relocatedPose.rotation;

        SetStartState(StartState.Normal);
    }

    public void ClickToSummonSonarAtCamera()
    {
        string prefabPathInResources = "Prefab/Prefab-Sonar";

        SetStartState(StartState.Summoning);

        GameObject prefab = Resources.Load<GameObject>(prefabPathInResources);
        if (prefab == null)
        {
            Debug.LogError("找不到 prefab：" + prefabPathInResources);
            return;
        }

        if (!sonarGO)
        {
            sonarGO = Instantiate(prefab);
        }

        float scale = GetModelScale(sonarGO);
        //绕z轴旋转180
        Quaternion rot180 = Quaternion.AngleAxis(180f, relocatedSonarPose.rotation * Vector3.forward);

        // 更新 Pose 的旋转
        relocatedSonarPose.rotation = rot180 * relocatedSonarPose.rotation;

        sonarGO.transform.position = relocatedSonarPose.position * scale;
        sonarGO.transform.rotation = relocatedSonarPose.rotation;
        
        
        //todo 删除临时代码
        Vector3 tempPos = new Vector3(-7.065f, -0.135f, 2.737f); //相对场景的坐标
        sonarGO.transform.position = ControllerRefer.SceneController.scene.transform.TransformPoint(tempPos);
        Quaternion rotationOffset = new Quaternion(0.0f, 0.156820267f, 0.0f, 0.987627208f); //相对场景的旋转
        sonarGO.transform.rotation = ControllerRefer.SceneController.scene.transform.rotation * rotationOffset;
        
        sonarGO.SetVisible(true);
    }

    //public void ClickToSummonSonar()
    //{
    //    Quaternion rot = Quaternion.Euler(0f, 0f, 0f);
    //    Matrix4x4 rotationMatrixTest = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one).inverse;
    //    world2Camera = world2Camera * rotationMatrixTest;
    //    return world2Camera;
    //}

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

    #region PC测试
    private Pose testPose()
    {
        float[,] temp = new float[4, 4]
        {
            { -0.994569278666888f, 0.009617385193789074f, 0.10363134580839106f, 0.46216198801994324f },
            { 0.9049069881439209f, -0.011102000251412392f, 0.425464004278183f, -1.8088890314102173f },
            { 0.42285001277923584f, -0.09018000215291977f, -0.9017009735107422f, 16.555131912231445f },
            { 0f, 0f, 0f, 1f }
        };

        return TransArrayToWorldPose(camPoseT0, temp);
    }

    public void tempGetPose()
    {
        UIStateManager.SetLoadingStatus(true);
        // Record Camera Pose
        Vector3 camPosition = arCamera.transform.position;
        Quaternion camRotation = arCamera.transform.rotation;
        camPoseT0 = Matrix4x4.TRS(camPosition, camRotation, Vector3.one);
        CountdownEvent countdownEvent = new CountdownEvent(1);
        bool hasError = false;
        StartCoroutine(WaitUnitCountDownComplete(countdownEvent, () =>
        {
            if (hasError)
            {
                SetStartState(StartState.Normal);
            }
            else
            {
                SetStartState(StartState.WaitSummon);
            }
            UIStateManager.SetLoadingStatus(false);
        }));
        ControllerRefer.SceneController.RequestSceneDataByKey(summary[selectedSceneIndex],
            onComplete: (result, response) =>
            {
                hasError |= !result;
                countdownEvent.Signal();
                ControllerRefer.VoiceController.InitLLMMessageList();
            });
    }
    
    public void tempClickSummon()
    {
        SetStartState(StartState.Summoning);
        // init modelInstance
        modelInstance = ControllerRefer.SceneController.AnalysisSceneData();
        relocatedPose = testPose();
        // set AstarPath ConsPos;
        Vector3 centerPos = AstarPath.active.data.recastGraph.forcedBoundsCenter;
        SMPLController.SetConsPos(centerPos);

        modelInstance.transform.position = relocatedPose.position;
        modelInstance.transform.rotation = relocatedPose.rotation;

        SetStartState(StartState.Normal);
    }

    //todo 好像是合并到ClickToGetPoseByCapture()里面？
    //public void ClickToGetPoseWithImage()
    //{
    //    buttonGetPose.GetComponent<Button>().interactable = false;
    //    buttonSummonAtCamera.gameObject.SetActive(false);

    //    string url = serverUrl;

    //    // Record Camera Pose
    //    Vector3 camPosition = arCamera.transform.position;
    //    Quaternion camRotation = arCamera.transform.rotation;
    //    camPoseT0 = Matrix4x4.TRS(camPosition, camRotation, Vector3.one);

    //    string imagePath = testImagePath;
    //    byte[] fileData = File.ReadAllBytes(imagePath);
    //    var texture = ExifUtil.FixOrientation(fileData);

    //    Debug.Log(imagePath.ToString());


    //    if (datasetLoc != null)
    //    {
    //        url = url + "request_NVLAD_redir/?source_location=" + datasetLoc.text;  //最后的url格式
    //    }

    //    StartCoroutine(UploadCapture(url, texture.EncodeToPNG()));

    //}

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
            Debug.LogError($"Failed to read image bytes: {e.Message}");
            return null;
        }
    }
    #endregion

    //public void ChangeMeshShaderWithTag(string tag, Shader targetShader)
    //{
    //    GameObject obj = GameObject.FindGameObjectWithTag(tag);
    //    MeshRenderer[] meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
    //    foreach (MeshRenderer meshRenderer in meshRenderers)
    //    {
    //        Material[] materials = meshRenderer.materials;
    //        foreach (Material material in materials)
    //        {
    //            material.shader = targetShader;
    //        }
    //    }
    //}

    public static float GetModelScale(GameObject modelInstance)
    {
        Transform mesh = modelInstance.transform.GetChildByName("mesh");
        if (mesh != null)
        {
            return mesh.localScale.x;
        }

        for (int i = 0; i < modelInstance.transform.childCount; i++)
        {
            var childTrans = modelInstance.transform.GetChild(i);
            if (childTrans.name.ToLower().Contains("FindPath".ToLower()))
            {
                continue;
            }
            return childTrans.localScale.x;
        }
        
        return modelInstance.transform.localScale.x;
    }


    public void ClickRotateR()
    {
        modelInstance.transform.RotateAround(modelInstance.transform.position, modelInstance.transform.right, 90f);
    }

    public void ClickRotateF()
    {
        modelInstance.transform.RotateAround(modelInstance.transform.position, modelInstance.transform.forward, 90f);
    }

    public void ClickRotateU()
    {
        modelInstance.transform.RotateAround(modelInstance.transform.position, modelInstance.transform.up, 90f);
    }

    private void SetStartState(StartState newState)
    {
        buttonGetPose.GetComponent<Button>().interactable = newState != StartState.GettingPos;
        buttonGetPose.gameObject.SetActive(newState != StartState.WaitSummon && newState != StartState.Summoning);
        buttonSummonAtCamera.gameObject.SetActive(newState == StartState.WaitSummon);
    }
    
    public void ChangeMeshShaderWithTag(string tag, Shader targetShader)
    {
        GameObject obj = GameObject.FindGameObjectWithTag(tag);
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

    private IEnumerator WaitUnitCountDownComplete(CountdownEvent countdownEvent, Action onComplete)
    {
        yield return new WaitUntil(() => countdownEvent.CurrentCount == 0);
        onComplete?.Invoke();
    }

    /// <summary>
    /// 获取当前选择的场景摘要数据
    /// </summary>
    /// <returns></returns>
    public SummaryItemData GetCurrentSummaryItemData()
    {
        return summary[selectedSceneIndex];
    }
    
    
    #region callback
    private void OnSceneSelectChanged(int index)
    {
        selectedSceneIndex = index;
        SetDataSetLoc();
    }
    
    #endregion callback
}
