using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Threading;

public class MeshController : MonoBehaviour
{
    public Camera arCamera;
    private GameObject modelInstance;

    public Dropdown sceneSelectDropdown;

    private Pose relocatedPose;

    public Button buttonGetPose;
    public Button buttonSummonAtCamera;

    public Button buttonHideMesh;
    public Button buttonShowMesh;

    public TMP_InputField datasetLoc;

    [Header("本地图片路径")]
    public string testImagePath;

    private Matrix4x4 camPoseT0, camPoseT1;

    private List<SummaryItemData> summary = new List<SummaryItemData>();
    private int selectedSceneIndex = 0;

    enum StartState
    {
        Normal,
        GettingPos,
        WaitSummon,
        Summoning
    }

    void Start()
    {
        sceneSelectDropdown.ClearOptions();
        // Fixme
        sceneSelectDropdown.onValueChanged.AddListener((value) => selectedSceneIndex = value);

        SetStartState(StartState.Normal);

        buttonHideMesh.gameObject.SetActive(true);
        buttonShowMesh.gameObject.SetActive(false);
    }

    public void InitSceneSummary(List<SummaryItemData> items)
    {
        summary = items;
        List<String> options = new List<string>();
        items.ForEach(item => options.Add(item.sceneName));
        sceneSelectDropdown.ClearOptions();
        sceneSelectDropdown.AddOptions(options);
    }

    public void ClickToSummonAtCamera()
    {
        SetStartState(StartState.Summoning);

        // init modelInstance
        modelInstance = FindObjectOfType<SceneController>().AnalysisSceneData();
        // set AstarPath ConsPos;
        Vector3 centerPos = AstarPath.active.data.recastGraph.forcedBoundsCenter;
        SMPLController.SetConsPos(centerPos);

        modelInstance.transform.position = relocatedPose.position;
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

    public void ClickToGetPoseByCapture()
    {
        SetStartState(StartState.GettingPos);

        // Record Camera Pose
        Vector3 camPosition = arCamera.transform.position;
        Quaternion camRotation = arCamera.transform.rotation;
        camPoseT0 = Matrix4x4.TRS(camPosition, camRotation, Vector3.one);

        arCamera.GetComponent<ARCameraManager>().TryAcquireLatestCpuImage(out XRCpuImage image);

        Texture2D renderTexture = new Texture2D(image.width, image.height, TextureFormat.BGRA32, false);
        XRCpuImage.ConversionParams conversionParams = new XRCpuImage.ConversionParams(image, TextureFormat.BGRA32);

        try
        {
            image.Convert(conversionParams, renderTexture.GetRawTextureData<byte>());
        }
        finally
        {
            image.Dispose();
        }
        renderTexture.Apply();

        byte[] rawData = renderTexture.EncodeToJPG();

        UIManager.SetLoadingStatus(true);
        CountdownEvent countdownEvent = new CountdownEvent(2);
        bool hasError = false;
        StartCoroutine(WaitUnitCountDownComplete(countdownEvent, () =>
        {
            if (hasError)
            {
                SetStartState(StartState.Normal);
            } else
            {
                SetStartState(StartState.WaitSummon);
            }
            UIManager.SetLoadingStatus(false);
        }));
        StartCoroutine(NetworkUtil.Instance.RelocateByCaptureRequest(datasetLoc?.text ?? "", rawData, 
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

        FindObjectOfType<SceneController>().RequestSceneDataByKey(summary[selectedSceneIndex].sceneKey,
            onComplete: isError =>
            {
                hasError = isError;
                countdownEvent.Signal();
            });
    }

    public void ClickToGetPoseWithImage()
    {
        SetStartState(StartState.GettingPos);

        // Record Camera Pose
        Vector3 camPosition = arCamera.transform.position;
        Quaternion camRotation = arCamera.transform.rotation;
        camPoseT0 = Matrix4x4.TRS(camPosition, camRotation, Vector3.one);

        string imagePath = testImagePath;
        byte[] rawData = ReadImageBytes(imagePath);

        Debug.Log(imagePath.ToString());

        StartCoroutine(NetworkUtil.Instance.RelocateByCaptureRequest(datasetLoc?.text ?? "", rawData,
            onSuccess: (res) => {
                // TODO Console res;
                relocatedPose = TransArrayToWorldPose(res);
                SetStartState(StartState.WaitSummon);
            },
            onFail: (errorText) => {
                // TODO Console Error
                SetStartState(StartState.Normal);
            }));

    }


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
        world2Camera = KeepModelYUp(world2Camera);
        var resModelPoseWorld = camPoseT0 * world2Camera;
        return new Pose(GetPosition(resModelPoseWorld), GetRotation(resModelPoseWorld));
    }

    public Pose TransferMatrix2Pose(Matrix4x4 camera2World)
    {
        Matrix4x4 world2Camera = camera2World.inverse;
        world2Camera = KeepModelYUp(world2Camera);

        Vector3 position = GetPosition(world2Camera);
        Quaternion rotation = world2Camera.rotation;
        return new Pose(position, rotation);
    }

    private Pose testPose()
    {
        float[,] temp = new float[4, 4]
        {
            { 0.04837900027632713f, 0.9958639740943909f, -0.07690999656915665f, 0.46216198801994324f },
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
        FindObjectOfType<SceneController>().RequestSceneDataByKey(summary[selectedSceneIndex].sceneKey,
            onComplete: isError =>
            {
                hasError = isError;
                countdownEvent.Signal();
            });
    }

    public void tempClickSummon()
    {
        SetStartState(StartState.Summoning);
        // init modelInstance
        modelInstance = FindObjectOfType<SceneController>().AnalysisSceneData();
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

    public void HideMeshRender()
    {
        GameObject scene = GameObject.FindGameObjectWithTag("Mesh");
        MeshRenderer sceneMeshRenderer = scene.GetComponentInChildren<MeshRenderer>();
        Material targetMat = Resources.Load<Material>("Materials/Occlusion_Material");
        sceneMeshRenderer.material = targetMat;

        buttonShowMesh.gameObject.SetActive(true);
        buttonHideMesh.gameObject.SetActive(false);
    }

    public void ShowMeshRender()
    {
        GameObject scene = GameObject.FindGameObjectWithTag("Mesh");
        MeshRenderer sceneMeshRenderer = scene.GetComponentInChildren<MeshRenderer>();
        Material targetMat = Resources.Load<Material>("Materials/GXL_Material");
        sceneMeshRenderer.material = targetMat;

        buttonShowMesh.gameObject.SetActive(false);
        buttonHideMesh.gameObject.SetActive(true);
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
            return fileData;
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
    public Matrix4x4 KeepModelYUp(Matrix4x4 world2Camera)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, 90f);
        Matrix4x4 rotationMatrixTest = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one).inverse;
        world2Camera = world2Camera * rotationMatrixTest;
        return world2Camera;
    }
}
