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

    private Pose relocatedPose;

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

    private Matrix4x4 camPoseT0, camPoseT1;

    [Header("本地图片路径")]
    public string testImagePath;

    void Start()
    {
        modelToSummon = (GameObject)Resources.Load("Prefab/Prefab-GXL"); // 在这里更换放置的模型
        SetDropDownAddListener(模型切换);
        // 模型选择.value = 1;
        defaultShader = Shader.Find("Universal Render Pipeline/Lit");
        hideShader = Shader.Find("VR/SpatialMapping/Occlusion");
        buttonGetPose.gameObject.SetActive(true);
        buttonSummonAtCamera.gameObject.SetActive(false);

        buttonHideMesh.gameObject.SetActive(true);
        buttonShowMesh.gameObject.SetActive(false);

        //videoScreen = GameObject.FindGameObjectWithTag("Screen");
        //sonar = GameObject.FindGameObjectWithTag("Sonar");
    }

    public void ClickToChangeMeshVisibility()
    {
        ChangeMeshVisibility("Scene");
        ChangeMeshVisibility("Sonar");
        
        isMeshVisible = !isMeshVisible;
    }

    // Change Mesh Visibility Under Obj with Tag
    private void ChangeMeshVisibility(string Tag) 
    {
        Shader newShader = isMeshVisible ? hideShader : defaultShader;

        GameObject scene = GameObject.FindGameObjectWithTag(Tag);
        //MeshRenderer sceneMeshRenderer = scene.GetComponentInChildren<MeshRenderer>();
        MeshRenderer[] meshRenderers = scene.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            Material[] materials = meshRenderer.materials;
            foreach (Material material in materials)
            {
                material.shader = newShader;
            }
        }
    }

    public void ClickToSummonAtCamera()
    {
        buttonGetPose.gameObject.SetActive(false);
        buttonSummonAtCamera.gameObject.SetActive(false);

        if (modelInstance == null)
        {
            modelInstance = Instantiate(modelToSummon, new Vector3(0, 0, 0), Quaternion.identity);
            Vector3 centerPos = AstarPath.active.data.recastGraph.forcedBoundsCenter;
            SMPLController.SetConsPos(centerPos);
        }

        modelInstance.transform.position = relocatedPose.position;
        modelInstance.transform.rotation = relocatedPose.rotation;

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

        // Record Camera Pose
        Vector3 camPosition = arCamera.transform.position;
        Quaternion camRotation = arCamera.transform.rotation;
        camPoseT0 = Matrix4x4.TRS(camPosition, camRotation, Vector3.one);

        string imagePath = testImagePath;
        byte[] rawData = ReadImageBytes(imagePath);

        Debug.Log(imagePath.ToString());


        if (datasetLoc != null)
        {
            url = url + "request_NVLAD_redir/?source_location=" + datasetLoc.text;  //最后的url格式
        }

        StartCoroutine(UploadCapture(url, rawData));

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

        float[,] num = new float[4, 4];
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
        relocatedPose = TransArrayToWorldPose(num);
        //for (int i = 0; i < num.GetLength(0); i++)
        //{
        //    for (int j = 0; j < num.GetLength(1); j++)
        //    {
        //        Debug.Log(num[i, j] + "\t");
        //    }
        //}

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

    public void tempGetPose()
    {
        buttonGetPose.gameObject.SetActive(false);
        buttonSummonAtCamera.gameObject.SetActive(true);
        Vector3 camPosition = arCamera.transform.position;
        Quaternion camRotation = arCamera.transform.rotation;
        camPoseT0 = Matrix4x4.TRS(camPosition, camRotation, Vector3.one);

    }

    public void tempClickSummon()
    {
        buttonGetPose.gameObject.SetActive(false);
        buttonSummonAtCamera.gameObject.SetActive(false);

        relocatedPose = testPose();

        if (modelInstance == null)
        {
            modelInstance = Instantiate(modelToSummon, new Vector3(0, 0, 0), Quaternion.identity);
            Vector3 centerPos = AstarPath.active.data.recastGraph.forcedBoundsCenter;
            SMPLController.SetConsPos(centerPos);
        }

        modelInstance.transform.position = relocatedPose.position;
        modelInstance.transform.rotation = relocatedPose.rotation;

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
        Material[] newMaterials = new Material[2];  // 假设你要设置两个材质

        // 给每个材质槽赋值
        newMaterials[0] = targetMat;
        newMaterials[1] = targetMat;

        // 设置到 MeshRenderer 上
        sceneMeshRenderer.materials = newMaterials;

        buttonShowMesh.gameObject.SetActive(true);
        buttonHideMesh.gameObject.SetActive(false);
    }

    public void ShowMeshRender()
    {
        GameObject scene = GameObject.FindGameObjectWithTag("Mesh");
        MeshRenderer sceneMeshRenderer = scene.GetComponentInChildren<MeshRenderer>();
        Material sceneMat = Resources.Load<Material>("Materials/GXL_Material");
        Material deskMat = Resources.Load<Material>("Materials/Desk_Material");
        Material[] newMaterials = new Material[2];  // 假设你要设置两个材质

        // 给每个材质槽赋值
        newMaterials[0] = sceneMat;
        newMaterials[1] = deskMat;

        // 设置到 MeshRenderer 上
        sceneMeshRenderer.materials = newMaterials;

        buttonShowMesh.gameObject.SetActive(true);
        buttonHideMesh.gameObject.SetActive(false);

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
    public Matrix4x4 KeepModelYUp(Matrix4x4 world2Camera)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, 90f);
        Matrix4x4 rotationMatrixTest = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one).inverse;
        world2Camera = world2Camera * rotationMatrixTest;
        return world2Camera;
    }

    public void ClickToSummonSonar()
    {
        //FindObjectOfType<MediaManager>().gameObject.SetActive(true);
        FindObjectOfType<MediaManager>().SummonSonar();
    }
}
