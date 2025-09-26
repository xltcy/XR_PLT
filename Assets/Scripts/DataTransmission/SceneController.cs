using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

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
    // use to search click trigger.
    private Dictionary<int, List<ActionBase>> clickTriggerActions = new Dictionary<int, List<ActionBase>>();
    // use to search imageRecognition trigger.
    private List<ActionTriggerData> imageRecognitionTrigggers = new List<ActionTriggerData>();

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
     * Send API to get summary data.
     */
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
     * Preprocess SceneData.
     * Instantiate SceneModel.
     * When a scene is selected & relocation is finished.
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
        InitAllTriggers();
        return scene;
    }

    /**
    * init globalTriggerCommands and pointTriggerCommands.
    * When initing SceneData.
    * Just load commands into list for further control, not trigger immediate.
    */
    private void InitAllTriggers()
    {
        // calculate each action's nextAction.
        var actions = new Dictionary<int, ActionBase>();
        foreach (var action in sceneData.globalActions)
        {
            actions.Add(action.id, action);
        }
        foreach (var point in sceneData.explanationPoints)
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

        globalTriggerCommands = GetTriggerCommands(sceneData.globalActions);
        foreach (var p in sceneData.explanationPoints)
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

        // init image Recognition triggers.
        imageRecognitionTrigggers.Clear();
        imageRecognitionTrigggers.AddRange(GetImageRecognitionTriggers(allActions));
        FindObjectOfType<TrackingImageManager>().InitTriggeredImage(imageRecognitionTrigggers);

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
                addObject.transform.position = scene.transform.TransformPoint(addAction.position);
                addObject.transform.rotation = scene.transform.rotation * addAction.GetRotationQuaternion();
                addObject.transform.localScale = addAction.scale;
                addObject.SetActive(false);
                addedObjects[addAction.id] = addObject;
                if (clickTriggerActions.ContainsKey(addAction.id))
                {
                    FindObjectOfType<Click3DObjectManager>().RegisteClickableObject(dynamicObject);
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
                    videoScreen.transform.SetParent(scene.transform, false);
                    videoScreen.transform.localPosition = videoAction.position;
                    videoScreen.transform.localRotation = videoAction.GetRotationQuaternion();
                    videoScreen.transform.localScale = videoAction.scale;
                }
                videoScreen.SetActive(true);
                var videoManager = FindObjectOfType<VideoManager>();
                videoManager.PlayVideo(videoAction.videoPath);
                videoManager.trackedImage = arTrackedImage;
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
                if (smplController != null && introduroduceAction != null && isStartAction)
                {
                    // only use in start action;stop action do nothing.
                    smplController.IntroduceString(introduroduceAction.introduction, onComplete: () =>
                    {
                        StartCoroutine(ConsoleAction(introduroduceAction, false));
                    });
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
}
