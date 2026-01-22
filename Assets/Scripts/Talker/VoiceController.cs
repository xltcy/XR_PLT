using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class VoiceController : BaseController
{
    // Start is called before the first frame update
    XunFeiYuYin xunfei;
    public Text debugText;
    public VoiceActiveButton voiceActiveButton;
    public SMPLController smplController;

    [Header("模拟语音识别结果")]
    public Text fakeVoiceText;

    private List<VoiceRecCommand> voiceRecCommands = new List<VoiceRecCommand>();
    public LLMGenerator llmGenerator;

    public override void OnRegister()
    {
        base.OnRegister();
        Init();
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
    }

    void Init()
    {
        if (voiceActiveButton)
        {
            voiceActiveButton.ResetBtn();
            voiceActiveButton.onPointerDown.AddListener(StartVoiceRecognize);
            voiceActiveButton.onPointerUp.AddListener(StopVoiceRecognize);
        }
        xunfei = XunFeiYuYin.Init("5c81de59", "ea4d5e9b06f8cfb0deae4d5360e7f8a7", "94348d7a6d5f3807176cb1f4923efa5c", "c6ea43c9e7b14d163bdeb4e51d2e564d");
        xunfei.语音识别完成事件 += ProcessVoiceRecognizeResult;

        llmGenerator = LLMGenerator.Init();

        // Registe commands
        voiceRecCommands.AddRange(VirHumanVoiceRecCommand.GetAllCommands());
        voiceRecCommands.AddRange(SceneVoiceRecCommand.GetAllCommands());
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

    public void RemoveVoiceRecCommands(List<VoiceRecCommand> commands)
    {
        voiceRecCommands.RemoveAll(item => commands.Contains(item));
    }

    public void ResetVoiceRecCommands()
    {
        voiceRecCommands.Clear();
    }

    public void StartVoiceRecognize()
    {
        xunfei.开始语音识别();
        SpeechManager.ForceStop();
    }
    public void StopVoiceRecognize()
    {
        StartCoroutine(xunfei.停止语音识别());
    }


    public void 清空文字()
    {
        debugText.text = "";
    }

    public void ProcessVoiceRecognizeResult(string result)
    {
        if (result.IsNullOrEmpty())
        {
            CommandFail();
            return;
        }

        if (debugText)
        {
            debugText.text += "\n语音识别结束，结果:" + result;
        }
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
                case VirHumanVoiceRecCommand virHumanCommand:
                    VirHumanAction(virHumanCommand);
                    break;
                case SceneVoiceRecCommand sceneCommand:
                    SceneAction(sceneCommand);
                    break;
                case ActionTriggerCommand triggerCommand:
                    ControllerRefer.SceneController.ConsoleVoiceTrigger(triggerCommand);
                    break;
                default:
                    matchFail = true;
                    RemoteChat(Prompt.GetCurSceneLLMQuestPrompt(result));
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
            ResetVoiceBtn();
        }
    }

    public void FakeGetVoiceResult(string result)
    {
        result = fakeVoiceText.text.ToString();
        ProcessVoiceRecognizeResult(result);
    }

    public void VirHumanAction(VirHumanVoiceRecCommand command)
    {

        switch (command.commandType)
        {
            case VirHumanVoiceRecCommand.VirHumanCommandType.shengNa:
                smplController.SetDestination(command.desLocalPosition); break;
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
            case SceneVoiceRecCommand.SceneCommandType.hideButton:
                smplController.HideButton(); break;
            case SceneVoiceRecCommand.SceneCommandType.showButton:
                smplController.ShowButton(); break;
            //case SceneVoiceRecCommand.SceneCommandType.screen:
            //    mediaManager.SummonCurtain();
            //    break;
            case SceneVoiceRecCommand.SceneCommandType.sonar:
                 break;
            case SceneVoiceRecCommand.SceneCommandType.separateSonar:
                break;
            case SceneVoiceRecCommand.SceneCommandType.recoverSonar:
                break;
            //case SceneVoiceRecCommand.SceneCommandType.showSonarWave:
            //    mediaManager.SummonSonarWithWave();
            //    mediaManager.SummonWall();
            //    break;
            //case SceneVoiceRecCommand.SceneCommandType.showSonarLabel:
            //    mediaManager.SummonSonarWithLabel(); break;
            //case SceneVoiceRecCommand.SceneCommandType.showFinding:
            //    mediaManager.SummonFindingsVideo();  break;
            case SceneVoiceRecCommand.SceneCommandType.end:
                SpeechManager.SayFromStr("好的，如果您还有兴趣了解更多内容，欢迎选择自由参观或向我提问"); break;
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

    //public void TestAddPlane()
    //{
    //    if (Plane == null)
    //    {
    //        Plane = Instantiate(_prefabOfPlane);
    //        Plane.transform.position = Camera.main.transform.position;
    //    }
    //}

    public void Reset()
    {
        清空文字();
    }

    public void Test()
    {
        清空文字();
    }

    private void ReconizeFail()
    {
        SpeechManager.SayFromStr("识别失败 请重新尝试");
        ResetVoiceBtn();
    }

    private void CommandFail()
    {
        SpeechManager.SayFromStr("出现错误 请重新尝试");
        ResetVoiceBtn();
    }

    private void RemoteChat(string userInput)
    {
        llmGenerator.CallForLLM(
            userInput,
            onSuccess: (string reply) =>
            {
                UnityEngine.Debug.Log("Msg in LLM:" + reply);
                SpeechManager.SayFromStr(reply);
                ResetVoiceBtn();
            },
            onError: (string error) =>
            {
                ReconizeFail();
            }
        );
    }

    //todo 优化调用
    public void InitLLMMessageList()
    {
        llmGenerator?.InitMessagesList();
    }

    private void ResetVoiceBtn()
    {
        if (voiceActiveButton) voiceActiveButton.ResetBtn();
        this.TriggerEvent(EventConstant.VOICE_RECOGNITION_END);
    }
}
