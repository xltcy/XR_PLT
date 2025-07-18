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

    private void DecomposePoseMatrix(Matrix4x4 pose, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        // 提取位置
        position = pose.GetColumn(3);

        // 提取缩放
        scale.x = new Vector4(pose.m00, pose.m01, pose.m02, 0).magnitude;
        scale.y = new Vector4(pose.m10, pose.m11, pose.m12, 0).magnitude;
        scale.z = new Vector4(pose.m20, pose.m21, pose.m22, 0).magnitude;

        // 提取旋转
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale).inverse * pose;
        rotation = rotationMatrix.rotation;
    }

    public void ClickToSummonAtCamera()
    {
        SetStartState(StartState.Summoning);

        // init modelInstance
        modelInstance = FindObjectOfType<SceneController>().AnalysisSceneData();
        // set AstarPath ConsPos;
        Vector3 centerPos = AstarPath.active.data.recastGraph.forcedBoundsCenter;
        SMPLController.SetConsPos(centerPos);

        //set model pos/rot/scale
        modelInstance.transform.parent = arCamera.transform;
        modelInstance.transform.localPosition = relocatedPose.position;
        modelInstance.transform.localRotation = relocatedPose.rotation;
        modelInstance.transform.RotateAround(arCamera.transform.position, arCamera.transform.right, 180f);
        modelInstance.transform.RotateAround(arCamera.transform.position, arCamera.transform.forward, 90f);
        modelInstance.transform.Rotate(new Vector3(180, 0, 0));
        arCamera.transform.DetachChildren();

        // Record Camera Pose
        Vector3 camPosition = arCamera.transform.position;
        Quaternion camRotation = arCamera.transform.rotation;
        camPoseT1 = Matrix4x4.TRS(camPosition, camRotation, Vector3.one);

        // fix jitter error
        Matrix4x4 deltaP = camPoseT1 * camPoseT0.inverse;
        Matrix4x4 modelPose = Matrix4x4.TRS(modelInstance.transform.position, modelInstance.transform.rotation ,modelInstance.transform.localScale);
        modelPose = deltaP.inverse * modelPose;
        Vector3 modelPosition, modelScale;
        Quaternion modelRotation;
        DecomposePoseMatrix(modelPose, out modelPosition, out modelRotation, out modelScale);
        modelInstance.transform.position = modelPosition;
        modelInstance.transform.rotation = modelRotation;

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
                relocatedPose = TransArrayToPose(res);
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

        string imagePath = "D:\\0-Desktop\\zwr\\test\\test.jpg";
        byte[] rawData = ReadImageBytes(imagePath);

        Debug.Log(imagePath.ToString());

        StartCoroutine(NetworkUtil.Instance.RelocateByCaptureRequest(datasetLoc?.text ?? "", rawData,
            onSuccess: (res) => {
                // TODO Console res;
                relocatedPose = TransArrayToPose(res);
                SetStartState(StartState.WaitSummon);
            },
            onFail: (errorText) => {
                // TODO Console Error
                SetStartState(StartState.Normal);
            }));

    }


    public Pose TransArrayToPose(float[,] num)
    {
        Matrix4x4 res = Matrix4x4.identity;

        for (int i = 0; i < num.GetLength(0); i++)
        {
            res.SetRow(i, new Vector4(num[i, 0], num[i, 1], num[i, 2], num[i, 3]));
        }

        res.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

        Debug.Log(res.ToString());

        Pose pose = TransferMatrix2Pose(res); //怎么获取返回值  

        return pose;
    }

    public Pose TransferMatrix2Pose(Matrix4x4 rtM)
    {
        var rtM_inverse = rtM.inverse;

        Vector3 position = GetPosition(rtM_inverse);

        Vector3 v = new Vector3();
        Quaternion q = GetRotation(rtM_inverse);

        v = q.eulerAngles;
        v.x *= -1;
        v.y = 180.0f - v.y;
        v.z = 180.0f + v.z;
        q = Quaternion.Euler(v);

        Quaternion rotation = q;

        return new Pose(position, rotation);
    }

    public Pose _TransferMatrix2Pose(Matrix4x4 rtM)
    {
        var rtM_inverse = rtM.inverse;

        Vector3 position = GetPosition(rtM_inverse);

        Vector3 v = new Vector3();
        Quaternion q = GetRotation(rtM_inverse);

        v = q.eulerAngles;
        v.x *= -1;
        v.y = 180.0f + v.y;
        v.z = 180.0f - v.z;
        q = Quaternion.Euler(v);

        Quaternion rotation = q;

        return new Pose(position, rotation);
    }

    private Pose testPose()
    {
        Matrix4x4 temp = Matrix4x4.identity;

        temp.SetRow(0, new Vector4(0.053799f, -0.891866f, 0.449088f, 11.775786f));
        temp.SetRow(1, new Vector4(-0.992680f, 0.000933f, 0.120771f, 0.481860f));
        temp.SetRow(2, new Vector4(-0.108130f, -0.452298f, -0.885288f, -15.738567f));
        temp.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

        Pose pose = _TransferMatrix2Pose(temp);

        return pose;
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

        relocatedPose = testPose();

        // init modelInstance
        modelInstance = FindObjectOfType<SceneController>().AnalysisSceneData();
        // set AstarPath ConsPos;
        Vector3 centerPos = AstarPath.active.data.recastGraph.forcedBoundsCenter;
        SMPLController.SetConsPos(centerPos);

        //set model pos/rot/scale
        modelInstance.transform.parent = arCamera.transform;
        modelInstance.transform.localPosition = relocatedPose.position;
        modelInstance.transform.localRotation = relocatedPose.rotation;
        modelInstance.transform.RotateAround(arCamera.transform.position, arCamera.transform.right, 180f);
        modelInstance.transform.RotateAround(arCamera.transform.position, arCamera.transform.forward, 90f);
        modelInstance.transform.Rotate(new Vector3(180, 0, 0));
        arCamera.transform.DetachChildren();

        // Record Camera Pose
        Vector3 camPosition = arCamera.transform.position;
        Quaternion camRotation = arCamera.transform.rotation;
        camPoseT1 = Matrix4x4.TRS(camPosition, camRotation, Vector3.one);

        // fix jitter error
        Matrix4x4 deltaP = camPoseT1 * camPoseT0.inverse;
        Matrix4x4 modelPose = Matrix4x4.TRS(modelInstance.transform.position, modelInstance.transform.rotation, modelInstance.transform.localScale);
        modelPose = deltaP.inverse * modelPose;
        Vector3 modelPosition, modelScale;
        Quaternion modelRotation;
        DecomposePoseMatrix(modelPose, out modelPosition, out modelRotation, out modelScale);
        modelInstance.transform.position = modelPosition;
        modelInstance.transform.rotation = modelRotation;

        SetStartState(StartState.Normal);
    }

    void FromMatrix(Transform trans, Matrix4x4 mat)
    {
        //3D scanner 需要调整子相机X轴旋转180,Z轴旋转90
        // 如果是移动模型的话，需要 v.z不变，子相机Z轴旋转-90（暂定）
        trans.position = GetPosition(mat);
        Vector3 v = new Vector3();
        Quaternion q = GetRotation(mat);
        v = q.eulerAngles;
        v.x = 180.0f - v.x;
        v.z *= 1;
        v.y = 180.0f - v.y;
        q = Quaternion.Euler(v);
        trans.rotation = q;
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
        var x = -matrix.m03;
        var y = matrix.m13;
        var z = matrix.m23;
        return new Vector3(x, y, z);
    }
    Vector3 GetScale(Matrix4x4 matrix)
    {
        var x = Mathf.Sqrt(matrix.m00 * matrix.m00 + matrix.m01 * matrix.m01 + matrix.m02 * matrix.m02);
        var y = Mathf.Sqrt(matrix.m10 * matrix.m10 + matrix.m11 * matrix.m11 + matrix.m12 * matrix.m12);
        var z = Mathf.Sqrt(matrix.m20 * matrix.m20 + matrix.m21 * matrix.m21 + matrix.m22 * matrix.m22);
        return new Vector3(x, y, z);
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
}
