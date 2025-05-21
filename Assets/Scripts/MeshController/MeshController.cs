using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Pathfinding;
using System.Text.RegularExpressions;
using UnityEngine.Events;
using static VirHumanVoiceRecCommand;
using static UnityEngine.GraphicsBuffer;
using System.Globalization;

public class MeshController : MonoBehaviour
{
    private GameObject modelToSummon;
    public Camera arCamera;
    private GameObject modelInstance;

    public Dropdown 模型选择;

    protected String[] oringinData = new String[10];
    private Matrix4x4 rtM;
    private Matrix4x4 rtM_inverse;

    private float[,] num = new float[3, 4];

    private static int 平移 = 1;
    private static int 旋转 = 2;
    private static int 缩放 = 3;

    private static float rotateSpeed = 0.1f;
    private static float translateSpeed = 0.001f;
    private static float scaleSpeed = 1.025f;

    private Vector2 oldPos1;
    private Vector2 oldPos2;

    private string poseStr = "";

    private int mode = 0;

    private const string serverUrl = "http://123.57.25.77:7005/media_app/"; 

    public Button buttonGetPose;
    public Button buttonSummonAtCamera;

    public Button buttonHideMesh;
    public Button buttonShowMesh;

    public TMP_InputField datasetLoc;

    public TextMeshProUGUI testText;


    public Shader hideShader;

    private Shader defaultShader;

    public List<Material> materials;

    private bool isMeshVisible = true;

    void Start()
    {
        modelToSummon = (GameObject)Resources.Load("Prefab/Prefab-GXL"); // 在这里更换放置的模型
        SetDropDownAddListener(模型切换);
        // 模型选择.value = 1;
        defaultShader = Shader.Find("Particles/Standard Surface");
        buttonGetPose.gameObject.SetActive(true);
        buttonSummonAtCamera.gameObject.SetActive(false);

        buttonHideMesh.gameObject.SetActive(true);
        buttonShowMesh.gameObject.SetActive(false);

        //videoScreen = GameObject.FindGameObjectWithTag("Screen");
        //sonar = GameObject.FindGameObjectWithTag("Sonar");
    }

    public void ClickToChangeMeshVisiblility()
    {
        Shader newShader = isMeshVisible ? hideShader : defaultShader;
        foreach (Material material in materials)
        {
            material.shader = newShader;
        }
        isMeshVisible = !isMeshVisible;
    }

    public void ClickToSummonAtCamera()
    {
        buttonGetPose.gameObject.SetActive(false);
        buttonSummonAtCamera.gameObject.SetActive(false);

        Pose pose = GetPoseByCapture();

        if (modelInstance != null)
        {
            modelInstance.transform.parent = arCamera.transform;
            modelInstance.transform.localPosition = pose.position;
            modelInstance.transform.localRotation = pose.rotation;
        }
        else
        {
            modelInstance = Instantiate(modelToSummon, new Vector3(0, 0, 0), Quaternion.identity);
            Vector3 centerPos = AstarPath.active.data.recastGraph.forcedBoundsCenter;
            SMPLController.SetConsPos(centerPos);
            modelInstance.transform.parent = arCamera.transform;
            modelInstance.transform.localPosition = pose.position;
            modelInstance.transform.localRotation = pose.rotation;
        }
        modelInstance.transform.RotateAround(arCamera.transform.position, arCamera.transform.right, 180f);
        modelInstance.transform.RotateAround(arCamera.transform.position, arCamera.transform.forward, 90f);
        //modelInstance.transform.localScale = new Vector3(1, -1, -1);
        modelInstance.transform.Rotate(new Vector3(180, 0, 0));

        arCamera.transform.DetachChildren();

        buttonGetPose.gameObject.SetActive(true);
        buttonSummonAtCamera.gameObject.SetActive(false);
    }

    public void ClickToGetInfo()
    {
        arCamera.GetComponent<ARCameraManager>().TryGetIntrinsics(out XRCameraIntrinsics intrinsics);

        testText.text = intrinsics.ToString();
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
        buttonGetPose.GetComponent<Button>().interactable = false;
        buttonSummonAtCamera.gameObject.SetActive(false);

        string url = serverUrl;

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


        //string imagePath = "F:\\UnityProjects\\Z_apks\\image1.jpg";
        //byte[] rawData = ReadImageBytes(imagePath);

        //Debug.Log(imagePath.ToString());


        if (datasetLoc != null)
        {
            url = url + "request_NVLAD_redir/?source_location=" + datasetLoc.text;  //最后的url格式
        }

        StartCoroutine(UploadCapture(url, rawData));

    }

    public void ClickToGetPoseWithImage()
    {
        buttonGetPose.GetComponent<Button>().interactable = false;
        buttonSummonAtCamera.gameObject.SetActive(false);

        string url = serverUrl;

        string imagePath = "F:\\UnityProjects\\Z_apks\\image1.jpg";
        byte[] rawData = ReadImageBytes(imagePath);

        Debug.Log(imagePath.ToString());


        if (datasetLoc != null)
        {
            url = url + "request_NVLAD_redir/?source_location=" + datasetLoc.text;  //最后的url格式
        }

        StartCoroutine(UploadCapture(url, rawData));

    }


    public Pose GetPoseByCapture()
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

    IEnumerator UploadCapture(string url, byte[] imageData)
    {

        string timestamp = "---------------------" + System.DateTime.Now.Ticks.ToString("x");
        byte[] boundaryByte = System.Text.Encoding.UTF8.GetBytes(timestamp);

        List<IMultipartFormSection> multipartSection = new List<IMultipartFormSection>();
        multipartSection.Add(new MultipartFormFileSection("images", imageData, "image.jpg", "image/jpg"));

        UnityWebRequest req = UnityWebRequest.Post(url, multipartSection, boundaryByte);

        req.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + timestamp);

        // send HTTP request
        yield return req.SendWebRequest();

        buttonGetPose.GetComponent<Button>().interactable = true;

        // 处理请求结果
        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Request succeeded. Response: " + req.downloadHandler.text);
            buttonGetPose.gameObject.SetActive(false);
            buttonSummonAtCamera.gameObject.SetActive(true);

        }
        else
        {
            Debug.LogError("Request failed. Error: " + req.error);
            Debug.Log(req.downloadHandler.text);
            buttonGetPose.gameObject.SetActive(true);
            buttonSummonAtCamera.gameObject.SetActive(false);
        }

        string response = req.downloadHandler.text;


        // 假设 receivedJson 是接收到的 JSON 字符串
        int startIndex = response.IndexOf("[[");
        int endIndex = response.IndexOf("]]");

        string truncatedJson = response.Substring(startIndex + 1, endIndex - startIndex + 1);

        Debug.Log("Truncated JSON: " + truncatedJson);

        string outerPattern = @"\[.*?\]"; // 匹配最外层的方括号内的内容
        string innerPattern = @"-?\d+\.\d+"; // 匹配一个浮点数

        MatchCollection outerMatches = Regex.Matches(truncatedJson, outerPattern);

        int rowIndex = 0;

        foreach (Match outerMatch in outerMatches)
        {
            string subJson = outerMatch.Value;

            MatchCollection innerMatches = Regex.Matches(subJson, innerPattern);

            int columnIndex = 0;

            foreach (Match innerMatch in innerMatches)
            {
                string numberString = innerMatch.Value;

                // 解析浮点数并设置到矩阵
                float number = float.Parse(numberString);
                num[rowIndex, columnIndex] = number;

                columnIndex++;
            }
            rowIndex++;
        }

        //for (int i = 0; i < num.GetLength(0); i++)
        //{
        //    for (int j = 0; j < num.GetLength(1); j++)
        //    {
        //        Debug.Log(num[i, j] + "\t");
        //    }
        //}

    }

    public Pose TransferMatrix2Pose(Matrix4x4 rtM)
    {
        rtM_inverse = rtM.inverse;

        Vector3 position = GetPosition(rtM_inverse);

        Vector3 v = new Vector3();
        Quaternion q = GetRotation(rtM_inverse);

        v = q.eulerAngles;
        //v.x = 180.0f - v.x;
        //v.z *= 1;
        //v.y = 180.0f - v.y;

        v.x *= -1;
        v.y = 180.0f - v.y;
        v.z = 180.0f + v.z;
        q = Quaternion.Euler(v);

        Quaternion rotation = q;

        return new Pose(position, rotation);
    }

    public Pose _TransferMatrix2Pose(Matrix4x4 rtM)
    {
        rtM_inverse = rtM.inverse;

        Vector3 position = GetPosition(rtM_inverse);

        Vector3 v = new Vector3();
        Quaternion q = GetRotation(rtM_inverse);

        v = q.eulerAngles;
        //v.x = 180.0f - v.x;
        //v.z *= 1;
        //v.y = 180.0f - v.y;

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

    public void tempGetPose()
    {
        buttonGetPose.gameObject.SetActive(false);
        buttonSummonAtCamera.gameObject.SetActive(true);
    }

    public void tempClickSummon()
    {
        buttonGetPose.gameObject.SetActive(false);
        buttonSummonAtCamera.gameObject.SetActive(false);

        Pose pose = GetPoseByCapture();

        if (modelInstance != null)
        {
            modelInstance.transform.parent = arCamera.transform;
            modelInstance.transform.localPosition = pose.position;
            modelInstance.transform.localRotation = pose.rotation;
        }
        else
        {
            modelInstance = Instantiate(modelToSummon, new Vector3(0, 0, 0), Quaternion.identity);
            Vector3 centerPos = AstarPath.active.data.recastGraph.forcedBoundsCenter;
            SMPLController.SetConsPos(centerPos);
            modelInstance.transform.parent = arCamera.transform;
            modelInstance.transform.localPosition = pose.position;
            modelInstance.transform.localRotation = pose.rotation;
        }
        modelInstance.transform.RotateAround(arCamera.transform.position, arCamera.transform.right, 180f);
        modelInstance.transform.RotateAround(arCamera.transform.position, arCamera.transform.forward, 90f);
        //modelInstance.transform.localScale = new Vector3(1, -1, -1);
        modelInstance.transform.Rotate(new Vector3(180, 0, 0));

        arCamera.transform.DetachChildren();

        buttonGetPose.gameObject.SetActive(true);
        buttonSummonAtCamera.gameObject.SetActive(false);
    }

    public GameObject scene;
    public void tempClickSummon2()
    {
        Pose pose = testPose();
        scene = Instantiate(scene, new Vector3(0, 0, 0), Quaternion.identity);
        scene.transform.parent = arCamera.transform;
        scene.transform.position = pose.position;
        scene.transform.localRotation = pose.rotation;

        arCamera.transform.DetachChildren();

        //modelInstance.transform.RotateAround(arCamera.transform.position, arCamera.transform.right, 180f);
        //modelInstance.transform.RotateAround(arCamera.transform.position, arCamera.transform.forward, 180f); //暂时封印
        //modelInstance.transform.localScale = new Vector3(1, -1, -1);

        scene.transform.RotateAround(arCamera.transform.position, arCamera.transform.right, 180f);
        scene.transform.RotateAround(arCamera.transform.position, arCamera.transform.forward, 90f);

        scene.transform.Rotate(new Vector3(180, 0, 0));

       // modelInstance.transform.rotation = Quaternion.Euler(modelInstance.transform.rotation.x, modelInstance.transform.rotation.y + 180, modelInstance.transform.rotation.z);

        arCamera.transform.DetachChildren();

        buttonGetPose.gameObject.SetActive(true);
        buttonSummonAtCamera.gameObject.SetActive(false);
        //FindObjectOfType<UIManager>().TransToSelectDesUI();
    }

    public void 模型切换(int v)
    {
        switch (v)
        {
            case 0: modelToSummon = (GameObject)Resources.Load("Prefab/Prefab-SJS-1226"); break;
            case 1: modelToSummon = (GameObject)Resources.Load("Prefab/Prefab-test717-1009"); break;
            default: break;
        }
    }

    public void 切换UI()
    {
        FindObjectOfType<UIManager>().TransToSelectDesUI();
    }

    void SetDropDownAddListener(UnityAction<int> OnValueChangeListener)
    {
        //模型选择.onValueChanged.AddListener((value) => {
        //    OnValueChangeListener(value);
        //});
    }

    void FromMatrix(Transform trans, Matrix4x4 mat)
    {
        //scannet数据集
        //v.x = 180 - v.x;
        //v.z *= -1;
        //v.y = 180.0f - v.y;

        //3D scanner 需要调整子相机X轴旋转180,Z轴旋转90
        // 如果是移动模型的话，需要 v.z不变，子相机Z轴旋转-90（暂定）
        trans.position = GetPosition(mat);
        //rtMText.text = mat.ToString();
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
        //Vector4 vy = matrix.GetColumn(1);
        //Vector4 vz = matrix.GetColumn(2);

        //Quaternion quaternion = Quaternion.LookRotation(new Vector3(vz.x, vz.y, vz.z), new Vector3(vy.x, vy.y, vy.z));
        float qw = Mathf.Sqrt(1f + matrix.m00 + matrix.m11 + matrix.m22) / 2;
        float w = 4 * qw;
        float qx = (matrix.m21 - matrix.m12) / w;
        float qy = (matrix.m02 - matrix.m20) / w;
        float qz = (matrix.m10 - matrix.m01) / w;

        return new Quaternion(qx, qy, qz, qw);
    }
    Vector3 GetPosition(Matrix4x4 matrix)
    {
        //var x = -matrix.m03;
        //var y = -matrix.m13;
        //var z = matrix.m23;
        //return new Vector3(x, y, z);
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

    public void 更换模式(int 模式)
    {
        mode = 模式;
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
        if (modelInstance)
        {
            //UpdateGraphTransform(modelInstance);
        }
        //make graph follow scene
        

        if (Input.touchCount == 0)
        {
            return;
        }
        
        if (mode == 平移)
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    Vector2 deltaPos = touch.deltaPosition;
                    Vector3 cameraDown = arCamera.transform.TransformVector(Vector3.down * deltaPos.y * translateSpeed);
                    Vector3 cameraRight = arCamera.transform.TransformVector(Vector3.right * deltaPos.x * translateSpeed);
                    modelInstance.transform.Translate(cameraDown + cameraRight);
                }
            }
        }
        else if (mode == 旋转)
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    Vector2 deltaPos = touch.deltaPosition;
                    modelInstance.transform.Rotate(Vector3.down * deltaPos.x * rotateSpeed, Space.World);
                    modelInstance.transform.Rotate(Vector3.right * deltaPos.y * rotateSpeed, Space.World);
                }
            }
        }
        else if (mode == 缩放)
        {
            if (Input.touchCount == 2)
            {
                if (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved)
                {
                    Vector2 newPos1 = Input.GetTouch(0).position;
                    Vector2 newPos2 = Input.GetTouch(1).position;
                    if (IsEnlarge(oldPos1, oldPos2, newPos1, newPos2))
                    {
                        float oldScale = modelInstance.transform.localScale.x;
                        float newScale = oldScale * scaleSpeed;
                        modelInstance.transform.localScale = new Vector3(newScale, newScale, newScale);
                    }
                    else
                    {
                        float oldScale = modelInstance.transform.localScale.x;
                        float newScale = oldScale / scaleSpeed;
                        modelInstance.transform.localScale = new Vector3(newScale, newScale, newScale);
                    }
                    oldPos1 = newPos1;
                    oldPos2 = newPos2;
                }
            }
        }
        else
        {
            return;
        }
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

    public void changeToVirtualManExhibition()
    {
        // Debug.Log("modelToSummon.gameObject.activeSelf" + modelInstance.gameObject.activeSelf);
        if (modelInstance && !modelInstance.gameObject.activeSelf)
        {
            modelInstance.SetActive(true);
        }
        //if (MySceneManager.instance != null)
        //{
        //    // MySceneManager.instance.ChangeToVirtualManExhibition();
        //    MySceneManager.instance.ChangeTo1818();
        //}
    }

}
