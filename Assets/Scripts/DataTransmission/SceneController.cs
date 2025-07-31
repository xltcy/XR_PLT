using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/**
 * Use to read json auto generate everything.
 */
public class SceneController : MonoBehaviour
{
    public enum GameObjectTag
    {
        Mesh,
        initPos,
    }
    [HideInInspector]
    public SummaryData summaryData;
    [HideInInspector]
    public SceneData sceneData;

    public Text jsonLocationHint;

    //public static string TEST_JSON_PC_HOME_PATH = "E:/Unity Proj/XR_PLT/";
    public static string TEST_JSON_PC_HOME_PATH = "H:/UnityProject/XR_PLT/";
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
    private String selectedExplainationPointId = "";
    private GameObject videoScreen;
    private ExplanationPoint selectedPoint
    {
        get { return sceneData.explanationPoints.FindLast(item => item.id == selectedExplainationPointId); }
    }

    private List<ActionBase> allActions
    {
        get
        {
            var list = new List<ActionBase>();
            list.AddRange(sceneData.globalActions);
            list.AddRange(selectedPoint.actions);
            return list;
        }
    }
    private GameObject scene;
    // Start is called before the first frame update
    void Start()
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
        RequireSummaryData();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /**
     * Send API Get Scene json data.
     * OnComplete only use to notify complete event.Param means hasError in http request.
     * All dispositions should be dispose in onSuccess/onError.
     */
    public void RequestSceneDataByKey(string sceneKey, Action<bool> onComplete = null)
    {
        localJsonPath = jsonHomePath + sceneKey;
        if (jsonLocationHint != null)
        {
            jsonLocationHint.text = "json应该放在：" + localJsonPath;
        }
        // TODO API get Response. Get From Local.
        // GetFakeResources();
        // get json
        StartCoroutine(NetworkUtil.Instance.GetSceneDataRequest(sceneKey,
            onSuccess: (res) => {
                if (sceneData == null || sceneData.timestampMs < res.timestampMs)
                {
                    // TODO save to local
                    sceneData = res;
                } else
                {
                    // use sceneData directly.
                }
                onComplete?.Invoke(false);
            },
            onFail: (errorText) => {
                //TODO
                onComplete?.Invoke(true);
            }));
    }

    /**
     * Set explaination point.
     */
    public void SetSelectedExplainationPoint(String explainationPointId)
    {
        var voiceController = FindObjectOfType<VoiceController>();
        if (selectedExplainationPointId.Length != 0)
        {
            // clear old info
            var oldCommands = GetTriggerCommandsByPoint(selectedExplainationPointId);
            voiceController.RemoveVoiceRecCommands(oldCommands);
        }
        selectedExplainationPointId = explainationPointId;
        var commands = GetTriggerCommandsByPoint(explainationPointId);
        voiceController.RegisteVoiceRecCommands(commands);

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

        FindObjectOfType<SMPLController>().SetDestination(selectedPoint.position, selectedPoint.initialIntroduction, selectedPoint.arriveIntroduction);
    }

    public void ConsoleVoiceTrigger(ActionTriggerCommand command)
    {
        var actionId = command.actionId;
        var pattern = command.matchPattern;
        ActionBase action = allActions.FindLast(item => item.id == actionId);
        StartCoroutine(ConsoleAction(action, isStartAction: pattern == action.startTrigger.matchPattern));
    }

    private void RequireSummaryData()
    {
        UIManager.SetLoadingStatus(true);
        StartCoroutine(NetworkUtil.Instance.GetSceneSummaryRequest(
            onSuccess: (res) => {
                summaryData = res;
                FindObjectOfType<MeshController>().InitSceneSummary(res.items);
                UIManager.SetLoadingStatus(false);
            },
            onFail: (errorText) => {
                //TODO
                UIManager.SetLoadingStatus(false);
            }));
    }

    private void GetFakeResources()
    {
        // todo use prefab

        // todo get json from local
        if (!File.Exists(localJsonPath))
        {
            Debug.LogError("找不到 scene.json！Path:" + localJsonPath);
            return;
        }

        string json = File.ReadAllText(localJsonPath);
        SceneData data = JsonConvert.DeserializeObject<SceneData>(json);
        Debug.Log("Load json: Data:" + data);
        sceneData = data;
    }

    /**
     * Preprocess SceneData.
     * Instantiate SceneModel
     */
    public GameObject AnalysisSceneData()
    {
        if (sceneData == null)
        {
            // todo error
            Debug.Log("No SceneData found!");
            return null;
        }
        // init object for SMPLController

        // load scene
        if (scene == null)
        {
            GameObject scenePrefab = (GameObject)Resources.Load("Prefab/" + sceneData.sceneModelPath);
            scene = Instantiate(scenePrefab);
            scene.tag = GameObjectTag.Mesh.ToString();
        }

        // initPos
        GameObject initPos = new GameObject("initPos");
        initPos.transform.SetParent(scene.transform, false);
        initPos.transform.localPosition = sceneData.initPosition;
        initPos.tag = GameObjectTag.initPos.ToString();

        // Generate ObjectDatas
        prefabs.Clear();
        foreach (var objectData in sceneData.objects)
        {
            GameObject prefab = (GameObject)Resources.Load("Prefab/" + objectData.url);
            prefabs[objectData.id] = prefab;
        }
        // explanationPoints. Use in selectDesController

        // Init voice commands
        InitAllTriggerCommands();
        return scene;
    }

    /**
     * Console Action & Check StartWithTrigger After Action.
     */
    private IEnumerator ConsoleAction(ActionBase actionData, bool isStartAction)
    {
        var trigger = isStartAction ? actionData.startTrigger : actionData.stopTrigger;
        if (trigger.delay > 0)
        {
            yield return new WaitForSeconds(trigger.delay);
        }
        switch (actionData.type)
        {
            case ActionType.GenerateObject:
                var addAction = actionData as AddObjectAction;
                var prefab = prefabs[addAction.objectDataId];
                GameObject addObject = Instantiate(prefab);
                addObject.AddComponent<DynamicObject>();
                addObject.transform.position = scene.transform.TransformPoint(addAction.position);
                addObject.transform.rotation = scene.transform.rotation * addAction.GetRotationQuaternion();
                addObject.transform.localScale = addAction.scale;
                addObject.SetActive(false);
                addedObjects[addAction.id] = addObject;
                // TODO clickTrigger auto add.
                if (sceneData.objects.Find(item => addAction.objectDataId == item.id)?.isClickable == true)
                {
                    FindObjectOfType<Click3DObjectManager>().RegisteClickableObject(addObject.GetComponent<ClickableObject>());
                }
                break;
            case ActionType.PlayVideo:
                // TODO
                var videoAction = actionData as PlayVideoAction;
                if (videoScreen == null)
                {
                    var videoPrefab = (GameObject)Resources.Load("Prefab/Prefab-Video");
                    videoScreen = Instantiate(videoPrefab);
                    videoScreen.transform.SetParent(scene.transform, false);
                }
                videoScreen.transform.localPosition = videoAction.position;
                videoScreen.transform.localRotation = videoAction.GetRotationQuaternion();
                videoScreen.transform.localScale = videoAction.scale;
                videoScreen.SetActive(true);
                FindObjectOfType<VideoManager>().PlayVideo(videoAction.videoPath);
                break;

            case ActionType.ObjectVisible:
            case ActionType.MoveObject:
            case ActionType.RotateObject:
            case ActionType.HighlightObject:
            case ActionType.Explosion:
                var objectAction = actionData as ObjectActionBase;
                var addedModel = addedObjects[objectAction.generateActionId];
                addedModel.GetComponent<DynamicObject>().ConsoleActions(objectAction, isStartAction, onComplete: () => { });
                break;
            case ActionType.Introduce:
                var introduroduceAction = actionData as IntroduceAction;
                var smplController = FindObjectOfType<SMPLController>();
                if (smplController != null && introduroduceAction != null)
                {
                    smplController.IntroduceString(introduroduceAction.introduction);
                }
                break;
            case ActionType.AvatarAnim:
                var avatarAnimAction = actionData as AvatarAnimAction;
                var smplCtrl = FindObjectOfType<SMPLController>();
                if (smplCtrl != null && avatarAnimAction != null)
                {
                    smplCtrl.AvatarAnim(avatarAnimAction.animTrigger);
                }
                break;
            default:
                throw new Exception($"Unknown action type: {actionData.type}");
        }

        foreach (var item in trigger.nextActionIds)
        {
            var nextAction = allActions.FindLast(i => i.id == item.Key);
            StartCoroutine(ConsoleAction(nextAction, item.Value));
        }
    }

    /**
     * init globalTriggerCommands and pointTriggerCommands
     */
    private void InitAllTriggerCommands()
    {
        // calculate each action's nextAction.
        var actions = new Dictionary<int, ActionBase>();
        foreach (var action in sceneData.globalActions)
        {
            actions.Add(action.id, action);
        }
        foreach(var point in sceneData.explanationPoints)
        {
            foreach (var action in point.actions)
            {
                actions.Add(action.id, action);
            }
        }

        foreach(var i in actions)
        {
            var action = i.Value;
            if (action.startTrigger.mode == TriggerMode.AfterAction)
            {
                var beforeAction = actions[action.startTrigger.afterActionId];
                var trigger = action.startTrigger.isWhenActionStart ? beforeAction.startTrigger : beforeAction.stopTrigger;
                //trigger.nextActionIds.Add(action.id, true);
                AddActionIntoNextActionDictionary(trigger.nextActionIds, action, true);
            }
            if (action.stopTrigger.mode == TriggerMode.AfterAction)
            {
                var beforeAction = actions[action.stopTrigger.afterActionId];
                var trigger = action.stopTrigger.isWhenActionStart ? beforeAction.startTrigger : beforeAction.stopTrigger;
                //trigger.nextActionIds.Add(action.id, false);
                AddActionIntoNextActionDictionary(trigger.nextActionIds, action, false);
            }
        }

        globalTriggerCommands = GetTriggerCommands(sceneData.globalActions);
        foreach (var p in sceneData.explanationPoints)
        {
            pointTriggerCommands[p.id] = GetTriggerCommands(p.actions);
        }
    }

    /**
     *  Get commands used in a point.Include global actions.
     */
    private List<VoiceRecCommand> GetTriggerCommandsByPoint(string pointId)
    {
        var list = new List<VoiceRecCommand>();
        list.AddRange(globalTriggerCommands);
        list.AddRange(pointTriggerCommands[pointId]);
        return list;
    }

    /**
     * Generate Voice Commands from actions.
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

    /**
     * Solve Error when an action's start & stop trigger after same actionID.
     * Start must before stop.
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
}
