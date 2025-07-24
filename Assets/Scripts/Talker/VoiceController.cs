using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class VoiceController : MonoBehaviour
{
    // Start is called before the first frame update
    XunFeiYuYin xunfei;
    public Text debugText;
    public VoiceActiveButton voiceActiveButton;
    public SMPLController smplController;

    [Header("模拟语音识别结果")]
    public Text fakeVoiceText;

    private List<VoiceRecCommand> voiceRecCommands = new List<VoiceRecCommand>();
    private LLMGenerator llmGenerator;

    void Start()
    {
        voiceActiveButton.ResetBtn();
        voiceActiveButton.onPointerDown.AddListener(开始语音识别);
        voiceActiveButton.onPointerUp.AddListener(停止语音识别);
        xunfei = XunFeiYuYin.Init("5c81de59", "ea4d5e9b06f8cfb0deae4d5360e7f8a7", "94348d7a6d5f3807176cb1f4923efa5c", "c6ea43c9e7b14d163bdeb4e51d2e564d");
        xunfei.语音识别完成事件 += 语音识别结果;

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
        debugText.text = "";
    }

    public void 语音识别结果(string result)
    {
        if (result == null || result == "")
        {
            CommandFail();
            return;
        }
        debugText.text += "\n语音识别结束，结果:" + result;
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
                    FindObjectOfType<SceneController>().ConsoleVoiceTrigger(triggerCommand);
                    break;
                default:
                    matchFail = true;
                    RemoteChat($"现在面前的是一个声呐，用户提问的问题是{result},请以一个精通声呐的专家身份回答");
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

    public void FakeGetVoiceResult(string result)
    {
        result = fakeVoiceText.text.ToString();
        语音识别结果(result);
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
            case SceneVoiceRecCommand.SceneCommandType.screen:
                break;
            case SceneVoiceRecCommand.SceneCommandType.sonar:
                 break;
            case SceneVoiceRecCommand.SceneCommandType.separateSonar:
                break;
            case SceneVoiceRecCommand.SceneCommandType.recoverSonar:
                break;
            case SceneVoiceRecCommand.SceneCommandType.showSonarWave:
                break;
            case SceneVoiceRecCommand.SceneCommandType.showSonarLabel:
                break;
            case SceneVoiceRecCommand.SceneCommandType.showFinding:
                break;
            default: ReconizeFail(); break;
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
