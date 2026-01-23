using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class VoiceController : BaseController
{
    // Start is called before the first frame update
    XunFeiYuYin xunfei;
    public SMPLController smplController;

    private List<VoiceRecCommand> voiceRecCommands = new List<VoiceRecCommand>();
    private LLMGenerator llmGenerator;

    public override void OnRegister()
    {
        base.OnRegister();
        Init();
    }

    public override void OnUnregister()
    {
        ResetVoiceRecCommands();
        base.OnUnregister();
    }

    void Init()
    {
        xunfei = XunFeiYuYin.Init("5c81de59", "ea4d5e9b06f8cfb0deae4d5360e7f8a7", "94348d7a6d5f3807176cb1f4923efa5c", "c6ea43c9e7b14d163bdeb4e51d2e564d");
        xunfei.语音识别完成事件 += ProcessVoiceRecognizeResult;

        llmGenerator = LLMGenerator.Init();

        // Registe commands
        ResetVoiceRecCommands();
        voiceRecCommands.AddRange(VirHumanVoiceRecCommand.GetAllCommands());
        voiceRecCommands.AddRange(SceneVoiceRecCommand.GetAllCommands());
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
    
    public void ProcessVoiceRecognizeResult(string result)
    {
        if (result.IsNullOrEmpty())
        {
            CommandFail();
            return;
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
            case SceneVoiceRecCommand.SceneCommandType.end:
                SpeechManager.SayFromStr("好的，如果您还有兴趣了解更多内容，欢迎选择自由参观或向我提问"); break;
            default: ReconizeFail(); break;
        }
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
        this.TriggerEvent(EventConstant.VOICE_RECOGNITION_END);
    }
}
