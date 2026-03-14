using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/**
 * Use to read json auto generate everything.
 */
public class SceneController : BaseController
{
    public enum GameObjectTag
    {
        Mesh,
    }
    public SummaryData SummaryData { get; private set; }
    public SceneData SceneData { get; private set; }
    
    public List<SummaryItemData> Summary { private set; get; }
    private SummaryItemData curSceneSummaryItemData;

    public Text jsonLocationHint;

    //public static string TEST_JSON_PC_HOME_PATH = "E:/Unity Proj/XR_PLT/";
    public static string TEST_JSON_PC_HOME_PATH = "G:/learning/core/gd/my_gd/XR_PLT";
    public static string TEST_JSON_ANDROID_HOME_PATH = "/storage/emulated/0/Download/";
    private string jsonHomePath = "";

    private static string JSON_NAME_GXL = "test-GXL.json";
    private static string JSON_NAME_HKG = "test-HKG.json";
    private string localJsonPath = "";
    // ObjectData objects' models.
    private Dictionary<String, GameObject> prefabs = new Dictionary<String, GameObject>();
    private Dictionary<int, GameObject> addedObjects = new Dictionary<int, GameObject>();
    private List<ActionTriggerCommand> globalTriggerCommands = new List<ActionTriggerCommand>();
    private Dictionary<string, List<ActionTriggerCommand>> pointTriggerCommands = new Dictionary<string, List<ActionTriggerCommand>>();
    // use to search click trigger.
    private Dictionary<int, List<ActionBase>> clickTriggerActions = new Dictionary<int, List<ActionBase>>();
    // use to search imageRecognition trigger.
    private List<ActionTriggerData> imageRecognitionTrigggers = new List<ActionTriggerData>();

    private String selectedExplainationPointId = "";
    private GameObject videoScreen;
    private ExplanationPoint selectedPoint
    {
        get { return SceneData.explanationPoints.FindLast(item => item.id == selectedExplainationPointId); }
    }

    private List<ActionBase> allActions
    {
        get
        {
            var list = new List<ActionBase>();
            list.AddRange(SceneData.globalActions);
            list.AddRange(selectedPoint.actions);
            return list;
        }
    }

    private GameObject _scene;
    public GameObject Scene
    {
        private set => _scene = value;
        get => _scene;
    }
    
    private Dictionary<string, GameObject> ModelDic = new Dictionary<string, GameObject>();

    #region 生命周期函数
    public override void OnRegister()
    {
        base.OnRegister();
        ManagerRefer.NetworkServiceManager.AddResponseListener(NetworkConstant.SUMMARY_JSON, RequireSummaryDataCallback);
        ManagerRefer.NetworkServiceManager.AddResponseListener(NetworkConstant.SCENE_DATA, RequestSceneDataByKeyCallback);
        
        InitiatePath();
        RequireSummaryData();
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
        ManagerRefer.NetworkServiceManager.RemoveResponseListener(NetworkConstant.SUMMARY_JSON, RequireSummaryDataCallback);
        ManagerRefer.NetworkServiceManager.RemoveResponseListener(NetworkConstant.SCENE_DATA, RequestSceneDataByKeyCallback);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #endregion 生命周期函数
    
    #region 获取数据
    void InitiatePath()
    {
        if (Application.platform == RuntimePlatform.Android) 
            jsonHomePath = TEST_JSON_ANDROID_HOME_PATH; 
        else 
            jsonHomePath = TEST_JSON_PC_HOME_PATH;
        
        string info = "1." + Application.persistentDataPath;
        info += "2." + Application.dataPath;
        info += "3." + Application.consoleLogPath;
        info += "4." + Application.streamingAssetsPath;
        info += "5." + Application.temporaryCachePath;
        if (jsonLocationHint!=null)
        {
            jsonLocationHint.text = info;
        }
    }
    
    #region 场景列表
    /// <summary>
    /// 获取场景列表
    /// </summary>
    public void RequireSummaryData()
    {
        //如果使用测试数据
        if (!DebugSwitch.Instance.DEBUG_USING_NETWORK_JSON)
        {
            GetSceneSummaryTestRequest(
                onSuccess: (res) =>
                {
                    SummaryData = res;
                    InitSceneSummary(res.items);
                },
                onFail:
                (errorText) =>
                {
                    //TODO
                }
            );
        }
        else
        {
            var requestParam = new Network.RequestParam.GetSummaryJson.RequestParam();
            requestParam.Send();
        }
    }

    /// <summary>
    /// 获取场景列表回调
    /// </summary>
    /// <param name="result"></param>
    /// <param name="response"></param>
    private void RequireSummaryDataCallback(bool result, NetworkResponse response)
    {
        if (result)
        {
            // 解析 JSON
            Debug.Log("下载成功: " + response.rawResponse);
            SummaryData = JsonConvert.DeserializeObject<SummaryData>(response.rawResponse);
            
            // 保存到本地文件
            string localPath = Path.Combine(Application.persistentDataPath, "downloaded-summary.json");
            File.WriteAllText(localPath, response.rawResponse);
            Debug.Log("文件保存到: " + localPath);
            
            InitSceneSummary(SummaryData.items);
        }
    }
    
    /// <summary>
    /// 获取本地测试场景列表
    /// </summary>
    /// <param name="onSuccess"></param>
    /// <param name="onFail"></param>
    public void GetSceneSummaryTestRequest(Action<SummaryData> onSuccess, Action<string> onFail)
    {
        // 创建模拟数据
        string localJsonPath = "test-summary.json";

        // Temp logic start
        // dont end with .json.
        var jsonString = Resources.Load<TextAsset>("Configs/" + "test-summary").text;
        if (jsonString != null)
        {
            SummaryData data = JsonConvert.DeserializeObject<SummaryData>(jsonString);
            onSuccess?.Invoke(data);
            return;
        }
        // temp logic end

        if (Application.platform == RuntimePlatform.Android)
        {
            localJsonPath = Application.persistentDataPath + localJsonPath;
        } else
        {
            localJsonPath = SceneController.TEST_JSON_PC_HOME_PATH + localJsonPath;
        }
        if (!File.Exists(localJsonPath))
        {
            string error = "找不到 scene.json！Path:" + localJsonPath;
            Debug.LogError(error);
            onFail.Invoke(error);
        }
        else
        {
            string json = File.ReadAllText(localJsonPath);
            SummaryData data = JsonConvert.DeserializeObject<SummaryData>(json);
            Debug.Log("Get Response json: Data:" + data);
            onSuccess.Invoke(data);
        }
    }
    #endregion 场景列表
    
    #region 某一场景数据
    
    /// <summary>
    /// 获取场景数据
    /// </summary>
    /// <param name="sceneItemData"></param>
    /// <param name="onComplete"></param>
    public void RequestSceneDataByKey(SummaryItemData sceneItemData , NetworkServiceManager.ResponseEvent onComplete = null)
    {
        if (sceneItemData == null)
        {
            Debug.LogError("sceneItemData为空，无法请求SceneData！请检查场景List");
            return;
        }
        
        localJsonPath = jsonHomePath + sceneItemData.sceneKey;
        if (jsonLocationHint != null)
        {
            jsonLocationHint.text = "json应该放在：" + localJsonPath;
        }

        if (!DebugSwitch.Instance.DEBUG_USING_NETWORK_JSON)
        {
            GetSceneDataTestRequest(sceneItemData, 
                onSuccess: (res) => {
                    if (SceneData == null || SceneData.sceneName != res.sceneName || SceneData.timestampMs < res.timestampMs)
                    {
                        // TODO save to local
                        SceneData = res;
                    } else
                    {
                        // use sceneData directly.
                    }
                    this.TriggerEvent(EventConstant.COMPLETE_GET_SCENE_DATA);
                    onComplete?.Invoke(true, null);
                },
                onFail: (errorText) => {
                    //TODO
                    onComplete?.Invoke(false, null);
                }
            );
        }
        else
        {
            var requestParam = new Network.RequestParam.GetSceneData.RequestParam(sceneItemData);
            requestParam.Send(null, onComplete);
        }
    }

    
    /// <summary>
    /// 获取场景数据回调
    /// </summary>
    /// <param name="result"></param>
    /// <param name="response"></param>
    private void RequestSceneDataByKeyCallback(bool result, NetworkResponse response)
    {
        if (!result)
        {
            return;
        }

        string jsonText = response.rawResponse;
        var sceneItemData = response.localData as SummaryItemData;
        
        // 解析 JSON
        SceneData data = JsonConvert.DeserializeObject<SceneData>(jsonText);
        
        // 保存到本地文件
        string localPath = Path.Combine(Application.persistentDataPath, $"{sceneItemData.sceneName}_{sceneItemData.sceneKey}.json");
        File.WriteAllText(localPath, jsonText);
        Debug.Log("文件保存到: " + localPath);
        
        if (SceneData == null || SceneData.sceneName != data.sceneName || SceneData.timestampMs < data.timestampMs)
        {
            SceneData = data;
        }

        this.TriggerEvent(EventConstant.COMPLETE_GET_SCENE_DATA);
    }
    
    /// <summary>
    /// 获取本地测试场景数据
    /// </summary>
    /// <param name="sceneItemData"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onFail"></param>
    public void GetSceneDataTestRequest(SummaryItemData sceneItemData, Action<SceneData> onSuccess, Action<string> onFail)
    {
        string localJsonPath = sceneItemData.sceneKey;

        // Temp logic start
        var jsonString = Resources.Load<TextAsset>("Configs/" + localJsonPath).text;
        if (jsonString != null)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter());
            var data = JsonConvert.DeserializeObject<SceneData>(jsonString, settings);
            onSuccess?.Invoke(data);
        } else
        {
            onFail?.Invoke("jsonFail");
        }
        
    }

    ///<summary>
    ///only  use for 太和殿
    /// </summary>
    public void LoadSceneData()
    {
        SummaryItemData sumId = new SummaryItemData
        {
            sceneName = "太和殿",
            sceneKey = "test-THD",
            sceneRelocateAlgo = "vggt_camera_locate"
        };
        string localJsonPath = sumId.sceneKey;

        // Temp logic start
        var jsonString = Resources.Load<TextAsset>("Configs/" + localJsonPath).text;
        if (jsonString != null)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter());
            var data = JsonConvert.DeserializeObject<SceneData>(jsonString, settings);
            SceneData = data;
            Debug.Log("Get Response json: Data:" + data);
        } else
        {
            Debug.Log("jsonFail");
        }
        GameObject prefab = Resources.Load<GameObject>("Prefab/" + SceneData.sceneModelPath);
        Scene = Instantiate(prefab);
        Scene.name = "THD";  
        Scene.tag = nameof(GameObjectTag.Mesh);
        Scene.transform.localRotation = Quaternion.Euler(0f, 0f, 180f) * Scene.transform.localRotation;
        GenerateBoundingBoxesForHighlightNodes();
    }
    #region 生成bounding_box
    
    // 只在 SceneController 里维护
    private readonly string[] autoBoundingBoxPaths =
    {
        "兽",
        "面阔"
    };
    private static readonly int[] BoundingBoxTriangles =
    {
        0, 1, 2, 0, 2, 3,        // Front
        4, 5, 6, 4, 6, 7,        // Back
        8, 9,10, 8,10,11,        // Left
        12,13,14, 12,14,15,      // Right
        16,17,18, 16,18,19,      // Top
        20,21,22, 20,22,23       // Bottom
    };

    private static readonly Vector3[] BoundingBoxNormals =
    {
        Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
        Vector3.back,    Vector3.back,    Vector3.back,    Vector3.back,
        Vector3.left,    Vector3.left,    Vector3.left,    Vector3.left,
        Vector3.right,   Vector3.right,   Vector3.right,   Vector3.right,
        Vector3.up,      Vector3.up,      Vector3.up,      Vector3.up,
        Vector3.down,    Vector3.down,    Vector3.down,    Vector3.down
    };
    // 视觉上略微放大一点
    [SerializeField] private float boundingBoxExpandFactor = 1.15f;
    [SerializeField] private Material boundingBoxGhostMaterial;
    private void GenerateBoundingBoxesForHighlightNodes()
    {
        if (Scene == null)
        {
            Debug.LogError("[BoundingBox] Scene not instantiated");
            return;
        }

        foreach (string path in autoBoundingBoxPaths)
        {
            GenerateBoundingBoxMeshForNode(path);
        }
    }

    private void GenerateBoundingBoxMeshForNode(string nodePath)
    {
        Transform node = FindTransformByPath(Scene.transform, nodePath);
        if (node == null)
        {
            Debug.LogWarning($"[BoundingBox] Node not found: {nodePath}");
            return;
        }
        //获取节点的MeshFilter组件
        MeshFilter sourceMf = node.GetComponent<MeshFilter>();
        if (sourceMf == null || sourceMf.sharedMesh == null)
        {
            Debug.LogWarning($"[BoundingBox] Node has no valid MeshFilter: {node.name}");
            return;
        }

        ApplyBoundingBoxMeshToNode(node, sourceMf, boundingBoxExpandFactor);
    }

    private void ApplyBoundingBoxMeshToNode(
        Transform node,
        MeshFilter sourceMf,
        float expandFactor
    )
    {
        // 不要污染原来的模型节点，单独创建一个子节点来放 bounding box
        string bboxName = node.name + "_BoundingBox";
        Transform bboxNode = node.Find(bboxName);

        GameObject bboxGo;
        if (bboxNode == null)
        {
            bboxGo = new GameObject(bboxName);
            bboxGo.transform.SetParent(node, false);
            bboxGo.transform.localPosition = Vector3.zero;
            bboxGo.transform.localRotation = Quaternion.identity;
            bboxGo.transform.localScale = Vector3.one;
        }
        else
        {
            bboxGo = bboxNode.gameObject;
        }

        // BoundingBox 自己的 MeshFilter / MeshRenderer
        MeshFilter bboxMf = bboxGo.GetComponent<MeshFilter>();
        if (bboxMf == null)
            bboxMf = bboxGo.AddComponent<MeshFilter>();

        MeshRenderer bboxMr = bboxGo.GetComponent<MeshRenderer>();
        if (bboxMr == null)
            bboxMr = bboxGo.AddComponent<MeshRenderer>();

        bboxMr.enabled = true;

        // 生成真正基于 sourceMf.bounds 的包围盒 Mesh
        Mesh bboxMesh = CreateBoundingBoxMesh(sourceMf, new Vector3(expandFactor, expandFactor, expandFactor));//可以根据不同轴进行扩张
        bboxMf.sharedMesh = bboxMesh;
        bboxMr.enabled = true;
        boundingBoxGhostMaterial = Resources.Load<Material>("Materials/BoundingBoxGhost");
        bboxMr.material = boundingBoxGhostMaterial;
        Debug.Log($"[BoundingBox] generated for node: {node.name}");
    }

    private static Mesh CreateBoundingBoxMesh(
        MeshFilter sourceMf,
        Vector3 expandScale
    )
    {
        if (sourceMf == null || sourceMf.sharedMesh == null)
        {
            Debug.LogError("[BoundingBox] Invalid source MeshFilter.");
            return null;
        }

        Bounds b = sourceMf.sharedMesh.bounds;
        Vector3 center = b.center;
        Vector3 half = Vector3.Scale(b.extents, expandScale);

        float minX = center.x - half.x;
        float maxX = center.x + half.x;
        float minY = center.y - half.y;
        float maxY = center.y + half.y;
        float minZ = center.z - half.z;
        float maxZ = center.z + half.z;

        Vector3[] vertices =
        {
            // Front (+Z)
            new Vector3(minX, minY, maxZ),
            new Vector3(maxX, minY, maxZ),
            new Vector3(maxX, maxY, maxZ),
            new Vector3(minX, maxY, maxZ),

            // Back (-Z)
            new Vector3(maxX, minY, minZ),
            new Vector3(minX, minY, minZ),
            new Vector3(minX, maxY, minZ),
            new Vector3(maxX, maxY, minZ),

            // Left (-X)
            new Vector3(minX, minY, minZ),
            new Vector3(minX, minY, maxZ),
            new Vector3(minX, maxY, maxZ),
            new Vector3(minX, maxY, minZ),

            // Right (+X)
            new Vector3(maxX, minY, maxZ),
            new Vector3(maxX, minY, minZ),
            new Vector3(maxX, maxY, minZ),
            new Vector3(maxX, maxY, maxZ),

            // Top (+Y)
            new Vector3(minX, maxY, maxZ),
            new Vector3(maxX, maxY, maxZ),
            new Vector3(maxX, maxY, minZ),
            new Vector3(minX, maxY, minZ),

            // Bottom (-Y)
            new Vector3(minX, minY, minZ),
            new Vector3(maxX, minY, minZ),
            new Vector3(maxX, minY, maxZ),
            new Vector3(minX, minY, maxZ),
        };

        Mesh mesh = new Mesh
        {
            name = $"{sourceMf.gameObject.name}_BoundingBoxMesh"
        };
        mesh.vertices = vertices;
        mesh.triangles = BoundingBoxTriangles;
        mesh.normals = BoundingBoxNormals;
        mesh.RecalculateBounds();

        return mesh;
    }

    private static Transform FindTransformByPath(
        Transform root,
        string path
    )
    {
        if (root == null || string.IsNullOrEmpty(path))
            return null;

        string[] nodes = path.Trim('/').Split('/');
        Transform current = root;

        foreach (string node in nodes)
        {
            current = current.Find(node);
            if (current == null)
                return null;
        }

        return current;
    }

    #endregion


    /// <summary>
    /// 上传场景数据
    /// </summary>
    /// <returns></returns>
    public void UploadSummaryData()
    {
        if (!DebugSwitch.Instance.DEBUG_USING_NETWORK_JSON)
        {
            return;
        }
        
        var curSceneItemData = GetCurrentSummaryItemData();
        if (curSceneItemData == null)
        {
            Debug.LogWarning("当前场景数据为空，无法上传");
            return;
        }

        var jsonText = Resources.Load<TextAsset>("Configs/" + localSceneDataJsonDict[curSceneItemData.sceneKey]).text;
        var reqParams = new Network.RequestParam.UploadSceneData.RequestParam(curSceneItemData.sceneKey, jsonText);
        reqParams.Send(null, (result, response) =>
        {
            if (result)
            {
                Debug.Log("上传成功: " + response.rawResponse);
            }
            else
            {
                Debug.LogError("上传失败: " + response.error);
            }
        });
    }
    
    private Dictionary<string, string> localSceneDataJsonDict = new Dictionary<string, string>()
    {
        {"4", "test-HKG"},
        {"3", "test-SJS"},
        {"6", "test-GXL"},
    };

    #endregion 某一场景数据
    
    /**
     * Preprocess SceneData.
     * Instantiate SceneModel.
     * When a scene is selected & relocation is finished.
     */
    public GameObject AnalysisSceneData()
    {
        if (SceneData == null)
        {
            // todo error
            Debug.Log("No SceneData found!");
            return null;
        }
        // init object for SMPLController

        // load scene
        if (Scene == null)
        {
            ManagerRefer.GameObjectPoolManager.Recycle(Scene);
            Scene = ManagerRefer.GameObjectPoolManager.Instantiate($"Prefab/{SceneData.sceneModelPath}");
            Scene.tag = nameof(GameObjectTag.Mesh);
        }

        // Generate ObjectDatas
        prefabs.Clear();
        foreach (var objectData in SceneData.objects)
        {
            GameObject prefab = ManagerRefer.GameObjectPoolManager.LoadPrefab($"Prefab/{objectData.url}");
            if (!prefab)
            {
                prefab = ManagerRefer.GameObjectPoolManager.LoadPrefab(objectData.url);
            }
            prefabs[objectData.id] = prefab;
        }
        // explanationPoints. Use in selectDesController

        // Init voice commands
        InitAllTriggers();
        return Scene;
    }
    
    #region 当前场景数据
    public void InitSceneSummary(List<SummaryItemData> items)
    {
        if (items == null)
        {
            Summary = new List<SummaryItemData>();
        }
        else
        {
            Summary = items;
        }
        
        // 通知场景总览数据初始化完成
        this.TriggerEvent(EventConstant.COMPLETE_INIT_SUMMARY);
    }
    
    
    /// <summary>
    /// 获取当前选择的场景摘要数据
    /// </summary>
    /// <returns></returns>
    public SummaryItemData GetCurrentSummaryItemData()
    {
        return curSceneSummaryItemData;
    }

    /// <summary>
    /// 设置当前选择的场景摘要数据
    /// </summary>
    /// <param name="data"></param>
    public void SetCurrentSummaryItemData(SummaryItemData data)
    {
        curSceneSummaryItemData = data;
    }
    #endregion 当前场景数据
    
    #endregion 获取数据
    
    /**
    * init globalTriggerCommands and pointTriggerCommands.
    * When initing SceneData.
    * Just load commands into list for further control, not trigger immediate.
    */
    private void InitAllTriggers()
    {
        // calculate each action's nextAction.
        var actions = new Dictionary<int, ActionBase>();
        foreach (var action in SceneData.globalActions)
        {
            actions.Add(action.id, action);
        }
        foreach (var point in SceneData.explanationPoints)
        {
            foreach (var action in point.actions)
            {
                actions.Add(action.id, action);
            }
        }

        // init actions relate lists by trigger type.
        foreach (var i in actions)
        {
            var action = i.Value;
            InitActionsByTriggerType(action.startTrigger, isStartTrigger: true, action, actions);
            InitActionsByTriggerType(action.stopTrigger, isStartTrigger: false, action, actions);
        }

        globalTriggerCommands = GetTriggerCommands(SceneData.globalActions);
        foreach (var p in SceneData.explanationPoints)
        {
            pointTriggerCommands[p.id] = GetTriggerCommands(p.actions);
        }
    }

    /**
     * Set explaination point with SceneData.
     * When an explaination point is selected.
     * Load voice commands into voiceController.
     */
    public void SetSelectedExplainationPoint(String explainationPointId)
    {
        if (selectedExplainationPointId.Length != 0)
        {
            // clear old info
            var oldCommands = GetTriggerCommandsByPoint(selectedExplainationPointId);
            ControllerRefer.VoiceController.RemoveVoiceRecCommands(oldCommands);
        }
        selectedExplainationPointId = explainationPointId;
        var commands = GetTriggerCommandsByPoint(explainationPointId);
        ControllerRefer.VoiceController.RegisteVoiceRecCommands(commands);

        // init image Recognition triggers.
        imageRecognitionTrigggers.Clear();
        imageRecognitionTrigggers.AddRange(GetImageRecognitionTriggers(allActions));
        ControllerRefer.TrackingImageManager.InitTriggeredImage(imageRecognitionTrigggers);

        // Immidiate Action
        allActions.ForEach(item =>
        {
            if (item.startTrigger.mode == TriggerMode.Immediate)
            {
                StartCoroutine(ConsoleAction(item, isStartAction: true));
            }
            if (item.stopTrigger.mode == TriggerMode.Immediate)
            {
                StartCoroutine(ConsoleAction(item, isStartAction: false));
            }
        });

        ControllerRefer.SMPLController.SetDestination(selectedPoint.position, selectedPoint.initialIntroduction, selectedPoint.arriveIntroduction);
    }

    public void ConsoleVoiceTrigger(ActionTriggerCommand command)
    {
        var actionId = command.actionId;
        var pattern = command.matchPattern;
        ActionBase action = allActions.FindLast(item => item.id == actionId);
        StartCoroutine(ConsoleAction(action, isStartAction: pattern == action.startTrigger.matchPattern));
    }

    public void ConsoleClickTrigger(int generateActionId, Click3DObjectManager.ClickAction newClickAction, bool isExit)
    {
        if (!clickTriggerActions.ContainsKey(generateActionId))
        {
            // dispatch
            return;
        }
        var actions = clickTriggerActions[generateActionId];
        // match action
        bool isStartAction;
        foreach(var action in actions)
        {
            if (action.IsClickTriggered(newClickAction, isExit, out isStartAction))
            {
                StartCoroutine(ConsoleAction(action, isStartAction));
            }
        }
    }

    public void ConsoleImageRecognizeTrigger(ActionTriggerData triggerData, ARTrackedImage trackedImage)
    {
        ActionBase action = allActions.FindLast(item => item.id == triggerData.originActionId);
        StartCoroutine(ConsoleAction(action, triggerData.isStartTrigger, trackedImage));
    }

    /**
     * Console Action & Check StartWithTrigger After Action.
     */
    private IEnumerator ConsoleAction(ActionBase actionData, bool isStartAction, ARTrackedImage arTrackedImage = null)
    {
        var trigger = isStartAction ? actionData.startTrigger : actionData.stopTrigger;
        if (trigger.delay > 0)
        {
            yield return new WaitForSeconds(trigger.delay);
        }
        Debug.Log("ConsoleAction: action id:" + actionData.id + " trigger mode:" + trigger.mode + " isStartAction:" + isStartAction + " arTrackedImage: " + arTrackedImage);
        switch (actionData.type)
        {
            case ActionType.GenerateObject:
                var addAction = actionData as AddObjectAction;
                var prefab = prefabs[addAction.objectDataId];
                GameObject addObject = Instantiate(prefab);
                var dynamicObject = addObject.AddComponent<DynamicObject>();
                dynamicObject.generateActionId = addAction.id;
                addObject.transform.position = Scene.transform.TransformPoint(addAction.position);
                addObject.transform.rotation = Scene.transform.rotation * addAction.GetRotationQuaternion();
                addObject.transform.localScale = addAction.scale;
                addObject.SetActive(false);
                addedObjects[addAction.id] = addObject;
                if (clickTriggerActions.ContainsKey(addAction.id))
                {
                    ControllerRefer.Click3DObjectManager.RegisteClickableObject(dynamicObject);
                }
                break;
            case ActionType.PlayVideo:
                // TODO
                var videoAction = actionData as PlayVideoAction;
                if (videoScreen == null)
                {
                    var videoPrefab = (GameObject)Resources.Load("Prefab/Prefab-Video");
                    videoScreen = Instantiate(videoPrefab);
                }
                if (arTrackedImage != null)
                {
                    //trackedImage原点：识别图的几何中心
                    //trackedImage.transform.right → 图片的水平方向 图像的宽度方向
                    //trackedImage.transform.up → 图片的竖直方向 图像的高度方向
                    //trackedImage.transform.forward → 图片的法线（垂直于图片） 法线方向（垂直于图片，指向相机这一侧）
                    // innerObject x 垂直视频向外， y 面向视频的上方, z 面向视频的左向 
                    // reset innerObject's transform to align with ARTrackedImage
                    videoScreen.transform.SetParent(arTrackedImage.transform, false);
                    var interObject = videoScreen.transform.Find("Screen");
                    interObject.transform.localScale = new Vector3(1, 1, 0.001f);
                    interObject.transform.localPosition = Vector3.zero;
                    interObject.transform.localRotation = Quaternion.Euler(90, 180, 0);
                } else
                {
                    videoScreen.transform.SetParent(Scene.transform, false);
                    videoScreen.transform.localPosition = videoAction.position;
                    videoScreen.transform.localRotation = videoAction.GetRotationQuaternion();
                    videoScreen.transform.localScale = videoAction.scale;
                }
                videoScreen.SetActive(true);
                var videoManager = videoScreen.GetComponent<VideoPlayComponent>();
                videoManager.PlayVideo(videoAction.videoPath);
                videoManager.trackedImage = arTrackedImage;
                break;

            case ActionType.ObjectVisible:
            case ActionType.MoveObject:
            case ActionType.RotateObject:
            case ActionType.HighlightObject:
            case ActionType.Explosion:
            case ActionType.WaveGenerate:
            case ActionType.CustomObjectFunction:
                var objectAction = actionData as ObjectActionBase;
                var addedModel = addedObjects[objectAction.generateActionId];
                addedModel.GetComponent<DynamicObject>().ConsoleActions(objectAction, isStartAction, onComplete: () => { });
                break;
            case ActionType.Introduce:
                var introduroduceAction = actionData as IntroduceAction;
                var smplController = ControllerRefer.SMPLController;
                if (introduroduceAction != null && isStartAction)
                {
                    // only use in start action;stop action do nothing.
                    smplController.IntroduceString(introduroduceAction.introduction, onComplete: () =>
                    {
                        Debug.LogWarning("[Console Action Debug]Coroutine in Coroutine IntroduceAction");
                        StartCoroutine(ConsoleAction(introduroduceAction, false));
                    });
                }
                break;
            case ActionType.AvatarAnim:
                var avatarAnimAction = actionData as AvatarAnimAction;
                var smplCtrl = ControllerRefer.SMPLController;
                if (avatarAnimAction != null)
                {
                    smplCtrl.AvatarAnim(avatarAnimAction.animTrigger);
                }
                break;
            case ActionType.ControllerFunction:
                ConsoleControllerFunction(actionData as ControllerFunctionAction, isStartAction);
                break;
            case ActionType.ProgramEvent:
                ConsoleProgramEvent(actionData as ProgramEventAction, isStartAction);
                break;
            default:
                throw new Exception($"Unknown action type: {actionData.type}");
        }

        foreach (var item in trigger.nextActionIds)
        {
            var nextAction = allActions.FindLast(i => i.id == item.Key);
            Debug.LogWarning("[Console Action Debug]Coroutine in Coroutine nextActionIds");
            StartCoroutine(ConsoleAction(nextAction, item.Value));
        }
    }
    
    /**
     *  Get commands used in a point.Include global actions.
     *  return a command list include global actions & pointId's actions.
     */
    private List<VoiceRecCommand> GetTriggerCommandsByPoint(string pointId)
    {
        var list = new List<VoiceRecCommand>();
        list.AddRange(globalTriggerCommands);
        list.AddRange(pointTriggerCommands[pointId]);
        return list;
    }

    /**
     * Return a trigger list include input actions' start & stop triggers.
     */
    private List<ActionTriggerCommand> GetTriggerCommands(List<ActionBase> actions)
    {
        var list = new List<ActionTriggerCommand>();
        if (actions == null || actions.Count == 0)
        {
            return list;
        }
        foreach (var action in actions)
        {
            var command = action.startTrigger.GetTriggerCommands(action.id);
            if (command != null)
            {
                list.Add(command);
            }
            command = action.stopTrigger.GetTriggerCommands(action.id);
            if (command != null)
            {
                list.Add(command);
            }
        }
        return list;
    }

    private List<ActionTriggerData> GetImageRecognitionTriggers(List<ActionBase> actions) 
    {
        var list = new List<ActionTriggerData>();
        foreach(var action in actions)
        {
            if (action.startTrigger.mode == TriggerMode.ImageRecognition)
            {
                list.Add(action.startTrigger);
            }
            if (action.stopTrigger.mode == TriggerMode.ImageRecognition)
            {
                list.Add(action.stopTrigger);
            }
        }
        return list;
    }

    /**
     * curTrigger: the trigger assume triggering curAction.
     * isStartTrigger: curTrigger is start trigger or not.
     * curAction: assume triggered action.
     * actions: <actionId, action data>.
     * If curTrigger is AfterAction, curAction will be added into before action's "nextActionIds" list.
     * If curTrigger is ClickObject, curAction will be added into clickTriggerActions.
     * Add here when a kind of trigger need to be inited before system work.
     */
    private void InitActionsByTriggerType(ActionTriggerData curTrigger, bool isStartTrigger, ActionBase curAction, Dictionary<int, ActionBase> actions)
    {
        // init ignored value.
        curTrigger.originActionId = curAction.id;
        curTrigger.isStartTrigger = isStartTrigger;

        // init nextActionIds
        if (curTrigger.mode == TriggerMode.AfterAction)
        {
            var beforeAction = actions[curTrigger.afterActionId];
            var beforeTrigger = curTrigger.isWhenActionStart ? beforeAction.startTrigger : beforeAction.stopTrigger;
            //trigger.nextActionIds.Add(action.id, true);
            AddActionIntoNextActionDictionary(beforeTrigger.nextActionIds, curAction, isStartTrigger);
        }

        // init clickTriggerActions
        if (curTrigger.mode == TriggerMode.ClickObject)
        {
            var generateActionId = curTrigger.generateActionId;
            if (!clickTriggerActions.ContainsKey(generateActionId))
            {
                clickTriggerActions.Add(generateActionId, new List<ActionBase>());
            }
            var list = clickTriggerActions[generateActionId];
            if (!list.Contains(curAction))
            {
                list.Add(curAction);
            }
        }
    }

    /**
     * Only for AfterAction type trigger.
     * Add desAction's id into preAction's trigger's nextActionIds.
     * 
     * desDict: <actionId, isStartAction> desDict is saved in trigger data model.
     * desAction: action need to be added into desDict.
     * isActionStart: desAction is start or stop when is triggered by desDict's owner trigger.
     * 
     * If desDict doesn't contain desAction, add actionId directly;
     * Otherwise, desAction's startTrigger & stopTrigger both need to be added into desDict,
     * in this condition, only trigger(<desAction's id, isActionStart==true>) will be added into desDict,
     * trigger(<desAction's id, isActionStart==false>) will be added into desAction's startTrigger's nextActionIds.
     */
    private void AddActionIntoNextActionDictionary(Dictionary<int, bool> desDict, ActionBase desAction ,bool isActionStart)
    {
        if (!desDict.ContainsKey(desAction.id))
        {
            desDict.Add(desAction.id, isActionStart);
            return;
        }
        desAction.stopTrigger.afterActionId = desAction.id;
        desAction.stopTrigger.isWhenActionStart = true;
        desAction.stopTrigger.delay -= desAction.startTrigger.delay;
        if (desAction.stopTrigger.delay < 0)
        {
            desAction.stopTrigger.delay = 0;
        }
        desAction.startTrigger.nextActionIds.Add(desAction.id, isActionStart);
    }
    
    #region 处理函数调用
    /// <summary>
    /// 自定义函数调用，直接调用
    /// </summary>
    /// <param name="actionData"></param>
    /// <param name="isStartAction"></param>
    public void ConsoleControllerFunction(ControllerFunctionAction actionData, bool isStartAction)
    {
        if (actionData.controllerName.IsNullOrEmpty())
        {
            return;
        }

        var script = ControllerRefer.GetByName(actionData.controllerName);
        if (!script)
        {
            return;
        }
        
        var functionName = actionData.controllerFunctionName;
        if (string.IsNullOrEmpty(functionName))
        {
            Debug.LogWarning($"action id:{actionData.id}-{functionName}为空");
            return;
        }

        try
        {
            var method = script.GetType().GetMethod(functionName, System.Reflection.BindingFlags.Public |
                                                                  System.Reflection.BindingFlags.Instance |
                                                                  System.Reflection.BindingFlags.NonPublic);
            if (method != null)
            {
                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length == 0)
                {
                    method.Invoke(script, null);
                }
                else if (parameters.Length == 1)
                {
                    // 直接传递参数，依赖类型兼容性
                    method.Invoke(script, new object[] { isStartAction });
                }
                else if (parameters.Length == 2)
                {
                    // 直接传递两个参数
                    method.Invoke(script, new object[] { isStartAction, actionData.controllerFunctionParam });
                }
            }
            else
            {
                Debug.LogWarning($"调用失败: action id:{actionData.id}-{functionName}() - 未找到对应函数");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"调用失败: action id{actionData.id}-{functionName}() - {e.Message}");
        }
    }
    #endregion 处理函数调用
    
    #region 处理程序事件
    public class ProgramEventData
    {
        public ProgramEventAction actionData;
        public bool isStartAction;
        
        public ProgramEventData(ProgramEventAction actionData, bool isStartAction)
        {
            this.actionData = actionData;
            this.isStartAction = isStartAction;
        }
    }
    
    /// <summary>
    /// 处理程序事件
    /// </summary>
    public void ConsoleProgramEvent(ProgramEventAction actionData, bool isStartAction)
    {
        if (actionData?.eventData == null) return;
        if (actionData.eventData.GetEventConstant().IsNullOrEmpty()) return;
        this.TriggerEvent(actionData.eventData.GetEventConstant(), new ProgramEventData(actionData, isStartAction));
    }
    #endregion 处理程序事件
}
