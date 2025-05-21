using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class VoiceController : MonoBehaviour
{
    // Start is called before the first frame update
    XunFeiYuYin xunfei;
    public Text text;
    public VoiceActiveButton voiceActiveButton;
    //public TrackingImageManager trackingImageManager;
    public SMPLController smplController;
    public GameObject target;
    public GameObject door;
    public GameObject sandwish;
    public GameObject gate;
    public GameObject ship;
    public GameObject chair;
    [Header("Prefabs")]
    public GameObject _prefabOfPlane;

    private List<VoiceRecCommand> voiceRecCommands = new List<VoiceRecCommand>();
    private LLMGenerator llmGenerator;

    #region PlaneComponent
    private GameObject _plane;
    private GameObject _mid;
    private GameObject _body;
    private GameObject _wingLeft;
    private GameObject _wingRight;

    //private BaiduLLMInterface baiduLLM;

    public GameObject Plane
    {
        set
        {
            _plane = value;
            _mid = _plane.transform.Find("Mid").gameObject;
            _body = _mid.transform.Find("Body").gameObject;
            _wingLeft = _mid.transform.Find("WingLeft").gameObject;
            _wingRight = _mid.transform.Find("WingRight").gameObject;
        }
        get
        {
            return _plane;
        }
    }
    #endregion

    void Start()
    {
        voiceActiveButton.ResetBtn();
        voiceActiveButton.onPointerDown.AddListener(开始语音识别);
        voiceActiveButton.onPointerUp.AddListener(停止语音识别);
        xunfei = XunFeiYuYin.Init("5c81de59", "ea4d5e9b06f8cfb0deae4d5360e7f8a7", "94348d7a6d5f3807176cb1f4923efa5c", "c6ea43c9e7b14d163bdeb4e51d2e564d");
        xunfei.语音识别完成事件 += 语音识别结果;

        llmGenerator = LLMGenerator.Init();

        // Registe commands
        voiceRecCommands.AddRange(IRVVoiceRecCommand.GetAllCommands());
        voiceRecCommands.AddRange(VirHumanVoiceRecCommand.GetAllCommands());
        voiceRecCommands.AddRange(SceneVoiceRecCommand.GetAllCommands());
        voiceRecCommands.AddRange(PlaneRelatedCommand.GetAllCommands());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RegisteCommand(VoiceRecCommand command)
    {
        voiceRecCommands.Add(command);
    }

    public void RegisteVoiceRecCommands(List<VoiceRecCommand> commands )
    {
        voiceRecCommands.AddRange(commands);
    }

    public void ResetVoiceRecCommands()
    {
        voiceRecCommands.Clear();
    }

    public void 开始语音识别()
    {
        xunfei.开始语音识别();
        SpeechManager.ForceStop();
    }
    public void 停止语音识别()
    {
        StartCoroutine(xunfei.停止语音识别());
    }


    public void 清空文字()
    {
        text.text = "";
    }

    public void 语音识别结果(string result)
    {
        if (result == null || result == "")
        {
            CommandFail();
            return;
        }
        text.text += "\n语音识别结束，结果:" + result;
        VoiceRecCommand resCommand = new VoiceRecCommand("");
        foreach (var command in voiceRecCommands)
        {
            Regex regex = new Regex(command.matchPattern);
            if (regex.IsMatch(result))
            {
                resCommand = command;
                break;
            }
        }
        bool matchFail = false;
        
        try
        {
            switch (resCommand)
            {
                //case IRVVoiceRecCommand iRVCommand:
                //    IamgeTrackConsole(iRVCommand);
                //    break;
                case VirHumanVoiceRecCommand virHumanCommand:
                    VirHumanAction(virHumanCommand);
                    break;
                case SceneVoiceRecCommand sceneCommand:
                    SceneAction(sceneCommand);
                    break;
                //case PlaneRelatedCommand planeRelatedCommand:
                //    PlaneRelatedAction(planeRelatedCommand);
                //    break;
                default:
                    matchFail = true;
                    RemoteChat("你好");
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("command exec error");
            CommandFail();
            throw;
        }
        if (!matchFail)
        {
            voiceActiveButton.ResetBtn();
        }
    }

    //public void IamgeTrackConsole(IRVVoiceRecCommand command)
    //{

    //    switch (command.commandType)
    //    {
    //        case IRVVoiceRecCommand.IRVCommandType.TRACK_ENABLE:
    //            trackingImageManager.EnableTrackImage(); break;
    //        case IRVVoiceRecCommand.IRVCommandType.TRACK_DISABLE:
    //            trackingImageManager.DisableTrackImage(); break;
    //        case IRVVoiceRecCommand.IRVCommandType.VIDEO_PLAY:
    //            trackingImageManager.PlayVideo(); break;
    //        case IRVVoiceRecCommand.IRVCommandType.VIDEO_PAUSE:
    //            trackingImageManager.PauseVideo(); break;
    //        case IRVVoiceRecCommand.IRVCommandType.VIDEO_STOP:
    //            trackingImageManager.StopVideo(); break;
    //        default: ReconizeFail(); break;
    //    }
    //}

    public void VirHumanAction(VirHumanVoiceRecCommand command)
    {

        switch (command.commandType)
        {
            case VirHumanVoiceRecCommand.VirHumanCommandType.shengNa:
                //StartCoroutine(smplController.moveToDestination(command)); break;
                smplController.SetDestination(command.desLocalPosition); break;
            //case VirHumanVoiceRecCommand.VirHumanCommandType.initializeSmpl:
            //    smplController.InitializeSmplPosition(); break;
            default: ReconizeFail(); break;
        }
    }

    public void SceneAction(SceneVoiceRecCommand command)
    {

        switch (command.commandType)
        {
            case SceneVoiceRecCommand.SceneCommandType.hideScene:
                smplController.HideMeshRender(); break;
            case SceneVoiceRecCommand.SceneCommandType.showScene:
                smplController.ShowMeshRender(); break;
            case SceneVoiceRecCommand.SceneCommandType.hideDropDown:
                smplController.HideDropDown(); break;
            case SceneVoiceRecCommand.SceneCommandType.showDropDown:
                smplController.ShowDropDown(); break;
            default: ReconizeFail(); break;
        }
    }

    //public void PlaneRelatedAction(PlaneRelatedCommand command)
    //{
    //    switch (command._commandType)
    //    {
    //        //case PlaneRelatedCommand.PlaneRelatedCommandType.showPlane:
    //        //    if (_plane == null)
    //        //    {
    //        //        SpeechManager.SayFromStr("飞机出现");
    //        //        _plane = Instantiate(_prefabOfPlane);
    //        //        _mid = _plane.transform.Find("Mid").gameObject;
    //        //        _body = _mid.transform.Find("Body").gameObject;
    //        //        _wingLeft = _mid.transform.Find("WingLeft").gameObject;
    //        //        _wingRight = _mid.transform.Find("WingRight").gameObject;
    //        //    }
    //        //    else
    //        //    {
    //        //        SpeechManager.SayFromStr("飞机已经出现");
    //        //    }
    //        //    break;
    //        case PlaneRelatedCommand.PlaneRelatedCommandType.explodePlane:
    //            if (_plane != null)
    //            {
    //                //SpeechManager.SayFromStr("一级爆炸");
    //                ModelTreeNode.OneDofExplosion(_plane);
    //                ModelTreeNode.OneDofExplosion(_mid);
    //            }
    //            else
    //            {
    //                SpeechManager.SayFromStr("飞机还没出现");
    //            }
    //            break;
    //        //case PlaneRelatedCommand.PlaneRelatedCommandType.explodeMid:
    //        //    SpeechManager.SayFromStr("二级爆炸");
    //        //    ModelTreeNode.OneDofExplosion(_mid);
    //        //    break;
    //        case PlaneRelatedCommand.PlaneRelatedCommandType.explodeBody:
    //            if (_plane != null)
    //            {
    //                //SpeechManager.SayFromStr("机身爆炸");
    //                ModelTreeNode.TwoDofExplosion(_body);
    //            }
    //            else
    //            {
    //                SpeechManager.SayFromStr("飞机还没出现");
    //            }
    //            break;
    //        //case PlaneRelatedCommand.PlaneRelatedCommandType.explodeWingLeft:
    //        //    SpeechManager.SayFromStr("左翼爆炸");
    //        //    ModelTreeNode.TwoDofExplosion(_wingLeft);
    //        //    break;
    //        //case PlaneRelatedCommand.PlaneRelatedCommandType.explodeWingRight:
    //        //    SpeechManager.SayFromStr("右翼爆炸");
    //        //    ModelTreeNode.TwoDofExplosion(_wingRight);
    //        //    break;
    //        case PlaneRelatedCommand.PlaneRelatedCommandType.explodWing:
    //            if (_plane != null)
    //            {
    //                //SpeechManager.SayFromStr("侧翼爆炸");
    //                ModelTreeNode.TwoDofExplosion(_wingLeft);
    //                ModelTreeNode.TwoDofExplosion(_wingRight);
    //            }
    //            else
    //            {
    //                SpeechManager.SayFromStr("飞机还没出现");
    //            }
    //            break;
    //        //case PlaneRelatedCommand.PlaneRelatedCommandType.debug:
    //        //    string debugStr;
    //        //    if (_plane.activeSelf == true)
    //        //        debugStr = "飞机已经激活了，位置是" + _plane.transform.position.ToString();
    //        //    else
    //        //        debugStr = "飞机还没激活呢";
    //        //    SpeechManager.SayFromStr(debugStr);
    //        //    break;
    //        default:
    //            ReconizeFail();
    //            break;
    //    }
    //}

    public void TestAddPlane()
    {
        if (Plane == null)
        {
            Plane = Instantiate(_prefabOfPlane);
            Plane.transform.position = Camera.main.transform.position;
        }
    }

    public void Reset()
    {
        清空文字();
    }

    public void Test()
    {
        清空文字();
    }

    //private void RemoteChat(string userInput)
    //{
    //    StartCoroutine(baiduLLM.ChatRequest(
    //        userInput,
    //        successAction: (string reply) =>
    //        {
    //            UnityEngine.Debug.Log("Msg in Reply:" + reply);
    //            SpeechManager.SayFromStr(reply);
    //            voiceActiveButton.ResetBtn();
    //        },
    //        failAction: (string error) =>
    //        {
    //            ReconizeFail();
    //        }
    //        ));
    //}

    private void ReconizeFail()
    {
        SpeechManager.SayFromStr("识别失败 请重新尝试");
        voiceActiveButton.ResetBtn();
    }

    private void CommandFail()
    {
        SpeechManager.SayFromStr("出现错误 请重新尝试");
        voiceActiveButton.ResetBtn();
    }

    private void RemoteChat(string userInput)
    {
        llmGenerator.CallForLLM(
            userInput,
            onSuccess: (string reply) =>
            {
                UnityEngine.Debug.Log("Msg in LLM:" + reply);
                SpeechManager.SayFromStr(reply);
                voiceActiveButton.ResetBtn();
            },
            onError: (string error) =>
            {
                ReconizeFail();
            }
        );
    }
}
