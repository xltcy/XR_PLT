using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Threading;
using Unity.Collections;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using static UnityEngine.UI.Button;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using static UnityEngine.GraphicsBuffer;
using static VirHumanVoiceRecCommand;
using UniGLTF;

public class MeshController : BaseController
{
    public Camera arCamera;
    private GameObject modelInstance;

    public Dropdown sceneSelectDropdown;

    private Pose relocatedPose;

    public Button buttonGetPose;
    public Button buttonSummonAtCamera;

    public Button buttonHideMesh;
    public Button buttonShowMesh;

    public Button buttonHideSonar;
    public Button buttonShowSonar;

    public TMP_InputField datasetLoc;

    [Header("本地图片路径")]
    public string testImagePath;

    private Matrix4x4 camPoseT0, camPoseT1;

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
        NetworkServiceSystem.Instance.AddResponseListener(NetworkConstant.GET_SONAR_POSE,GetPoseCallBack);
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
        sceneSelectDropdown.onValueChanged.RemoveListener(OnSceneSelectChanged);
        NetworkServiceSystem.Instance.RemoveResponseListener(NetworkConstant.GET_SONAR_POSE, GetPoseCallBack);
    }

    void Start()
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
    
    void OnEnable()
    {

    }
    
    void OnDisable()
    {

    }

    public void InitSceneSummary(List<SummaryItemData> items)
    {
        summary = items;
        List<String> options = new List<string>();
        items.ForEach(item => options.Add(item.sceneName));
        sceneSelectDropdown.ClearOptions();
        sceneSelectDropdown.AddOptions(options);
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
            if (childTrans.name.ToLower().Contains("FindPath"))
            {
                continue;
            }
            return childTrans.localScale.x;
        }
        
        return modelInstance.transform.localScale.x;
    }

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
        string prefabPathInResources = "Prefab/3ds_sonar";

        SetStartState(StartState.Summoning);

        GameObject prefab = Resources.Load<GameObject>(prefabPathInResources);
        if (prefab == null)
        {
            Debug.LogError("找不到 prefab：" + prefabPathInResources);
            return;
        }

        modelInstance = Instantiate(prefab);

        float scale = GetModelScale(modelInstance);
        //绕z轴旋转180
        Quaternion rot180 = Quaternion.AngleAxis(180f, relocatedPose.rotation * Vector3.forward);

        // 更新 Pose 的旋转
        relocatedPose.rotation = rot180 * relocatedPose.rotation;

        modelInstance.transform.position = relocatedPose.position * scale;
        modelInstance.transform.rotation = relocatedPose.rotation;

        SetStartState(StartState.Normal);
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
        UIManager.SetLoadingStatus(true);

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
            // load from file
            rawData = ReadImageBytes(testImagePath);
        }
        switch (relocateType)
        {
            case RelocateType.Scene:
                SendImageAndReadJson(rawData);
                break;
            case RelocateType.Sonar:
                SendImage(rawData);
                break;
        }
        Debug.Log(testImagePath.ToString());
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
            UIManager.SetLoadingStatus(false);
        }));
        StartCoroutine(NetworkUtil.Instance.RelocateByCaptureRequest(summary[selectedSceneIndex], datasetLoc?.text ?? "", rawData,
            onSuccess: (res) => {
                // TODO Console res;
                relocatedPose = TransArrayToWorldPose(res);
                countdownEvent.Signal();

            },
            onFail: (errorText) => {
                // TODO Console Error
                hasError = true;
                countdownEvent.Signal();
            }));
        
        ControllerRefer.SceneController.RequestSceneDataByKey(summary[selectedSceneIndex],
            onComplete: (result, response) =>
            {
                hasError |= !result;
                countdownEvent.Signal();
                ControllerRefer.VoiceController.InitLLMMessageList();
            });
    }

    private void SendImage(byte[] rawData)
    {
        bool hasError = false;
        SetStartState(StartState.GettingPos);
        UIManager.SetLoadingStatus(true);

        var req = new GetSonarPoseParams("sonar", rawData);
        req.Send();
    }
    [Serializable]
    public class GetSonarPoseResponseData
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
            if (poseMatrix == null) return Matrix4x4.identity;

            Matrix4x4 m = Matrix4x4.identity;

            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 4; c++)
                    m[r, c] = poseMatrix[r, c];

            // 补最后一行
            m[3, 0] = 0; m[3, 1] = 0; m[3, 2] = 0; m[3, 3] = 1;

            return m;
        }

        /// <summary>
        /// 转成 Unity Pose
        /// </summary>
        public Pose ToPose()
        {
            Matrix4x4 m = ToMatrix();
            Vector3 position = m.GetColumn(3);
            Quaternion rotation = Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
            return new Pose(position, rotation);
        }
    }
    private void GetPoseCallBack(bool result, NetworkResponse response)
    {
        if (result)
        {
            var data = JsonUtility.FromJson<GetSonarPoseResponseData>(response.rawResponse);
            Debug.Log(data.message);

            // 2️⃣ 手动解析 pose 字段
            data.ParsePoseFromJson(response.rawResponse);

            // 3️⃣ 转 Matrix4x4
            //Matrix4x4 matrix = data.ToMatrix();

            // 4️⃣ 转 Pose
            relocatedPose = TransArrayToWorldPose(data.poseMatrix);

            // 5️⃣ 输出测试
            Debug.Log($"Position: {relocatedPose.position}, Rotation: {relocatedPose.rotation}");
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


    public Pose TransArrayToWorldPose(float[,] num)
    {
        Matrix4x4 res = Matrix4x4.identity;
        for (int i = 0; i < num.GetLength(0); i++)
        {
            res.SetRow(i, new Vector4(num[i, 0], num[i, 1], num[i, 2], num[i, 3]));
        }
        res.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
        Debug.Log(res.ToString());

        Matrix4x4 world2Camera = res.inverse;
        // world2Camera = KeepModelYUp(world2Camera);
        var resModelPoseWorld = camPoseT0 * world2Camera;
        return new Pose(GetPosition(resModelPoseWorld), GetRotation(resModelPoseWorld));
    }

    private Pose testPose()
    {
        float[,] temp = new float[4, 4]
        {
            { -0.994569278666888f, 0.009617385193789074f, 0.10363134580839106f, 0.46216198801994324f },
            { 0.9049069881439209f, -0.011102000251412392f, 0.425464004278183f, -1.8088890314102173f },
            { 0.42285001277923584f, -0.09018000215291977f, -0.9017009735107422f, 16.555131912231445f },
            { 0f, 0f, 0f, 1f }
        };

        return TransArrayToWorldPose(temp);
    }

    private void SetStartState(StartState newState)
    {
        buttonGetPose.GetComponent<Button>().interactable = newState != StartState.GettingPos;
        buttonGetPose.gameObject.SetActive(newState != StartState.WaitSummon && newState != StartState.Summoning);
        buttonSummonAtCamera.gameObject.SetActive(newState == StartState.WaitSummon);
    }

    public void tempGetPose()
    {
        UIManager.SetLoadingStatus(true);
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
            UIManager.SetLoadingStatus(false);
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
    Quaternion GetRotation(Matrix4x4 matrix)
    {
        float qw = Mathf.Sqrt(1f + matrix.m00 + matrix.m11 + matrix.m22) / 2;
        float w = 4 * qw;
        float qx = (matrix.m21 - matrix.m12) / w;
        float qy = (matrix.m02 - matrix.m20) / w;
        float qz = (matrix.m10 - matrix.m01) / w;

        return new Quaternion(qx, qy, qz, qw);
    }
    Vector3 GetPosition(Matrix4x4 matrix)
    {
        return matrix.GetColumn(3);
    }
    Vector3 GetScale(Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = matrix.GetColumn(0).magnitude;
        scale.y = matrix.GetColumn(1).magnitude;
        scale.z = matrix.GetColumn(2).magnitude;
        return scale;
    }

    private void DecomposePoseMatrix(Matrix4x4 pose, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        // 提取位置
        position = GetPosition(pose);

        // 提取缩放
        scale = GetScale(pose);

        // 提取旋转
        rotation = GetRotation(pose);
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

    private bool IsEnlarge(Vector2 oP1, Vector2 oP2, Vector2 nP1, Vector2 nP2)
    {
        float length1 = Mathf.Sqrt((oP1.x - oP2.x) * (oP1.x - oP2.x) + (oP1.y - oP2.y) * (oP1.y - oP2.y));
        float length2 = Mathf.Sqrt((nP1.x - nP2.x) * (nP1.x - nP2.x) + (nP1.y - nP2.y) * (nP1.y - nP2.y));
        if (length1 < length2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void Update()
    {
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
            Debug.LogError($"Failed to read image bytes: {e.Message}");
            return null;
        }
    }

    private IEnumerator WaitUnitCountDownComplete(CountdownEvent countdownEvent, Action onComplete)
    {
        yield return new WaitUntil(() => countdownEvent.CurrentCount == 0);
        onComplete?.Invoke();
    }

    //public void ClickToSummonSonar()
    //{
    //    Quaternion rot = Quaternion.Euler(0f, 0f, 0f);
    //    Matrix4x4 rotationMatrixTest = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one).inverse;
    //    world2Camera = world2Camera * rotationMatrixTest;
    //    return world2Camera;
    //}
    
    
        
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
    }
    
    #endregion callback
}
