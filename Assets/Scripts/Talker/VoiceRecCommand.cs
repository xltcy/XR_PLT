using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Base class.
 * Use for voice recognize command.
 * Use MySceneManager.TestRegexMatch to test regex pattern.
 * Test function already bind with "return to record" button in ModelExhibition Scene.
 */
public class VoiceRecCommand
{
    // reserve
    public int id;
    // reserve
    public string info;
    // use to match with voice recognize result.
    public string matchPattern = "";
    public VoiceRecCommand(string info)
    {
        this.info = info;
    }
    public VoiceRecCommand(string info, string matchPattern)
    {
        this.info = info;
        this.matchPattern = matchPattern;
    }
}

/**
 * Use for Track Image related command.
 */
public class IRVVoiceRecCommand: VoiceRecCommand
{
    public IRVCommandType commandType;
    public IRVVoiceRecCommand(string info, IRVCommandType type) : base(info)
    {
        switch(type)
        {
            case IRVCommandType.TRACK_ENABLE:
                matchPattern = "开始识别(?!并播放)";break;
            case IRVCommandType.TRACK_DISABLE:
                matchPattern = "关闭识别";break;
            case IRVCommandType.VIDEO_PLAY:
                matchPattern = "开始播放"; break;
            case IRVCommandType.VIDEO_PAUSE:
                matchPattern = "暂停播放"; break;
            case IRVCommandType.VIDEO_STOP:
                matchPattern = "停止播放"; break;
            case IRVCommandType.TRACK_ENABLE_AUTO_PLAY_VIDEO:
                matchPattern = "开始识别并播放"; break;
        }
        commandType = type;
    }

    // TRACK_ENABLE_AUTO_PLAY_VIDEO is not used.
    public static List<IRVVoiceRecCommand> GetAllCommands()
    {
        List<IRVVoiceRecCommand> res = new List<IRVVoiceRecCommand>();
        res.Add(new IRVVoiceRecCommand("", IRVCommandType.TRACK_ENABLE));
        res.Add(new IRVVoiceRecCommand("", IRVCommandType.TRACK_DISABLE));
        res.Add(new IRVVoiceRecCommand("", IRVCommandType.VIDEO_PLAY));
        res.Add(new IRVVoiceRecCommand("", IRVCommandType.VIDEO_PAUSE));
        res.Add(new IRVVoiceRecCommand("", IRVCommandType.VIDEO_STOP));
        // res.Add(new IRVVoiceRecCommand("", IRVCommandType.TRACK_ENABLE_AUTO_PLAY_VIDEO));
        return res;
    }

    public enum IRVCommandType
    {
        TRACK_ENABLE,
        TRACK_DISABLE,
        VIDEO_PLAY,
        VIDEO_PAUSE,
        VIDEO_STOP,
        TRACK_ENABLE_AUTO_PLAY_VIDEO
    }
}


public class VirHumanVoiceRecCommand : VoiceRecCommand
{
    public VirHumanCommandType commandType;

    public Vector3 desLocalPosition;

    public string introduction = "";
    public VirHumanVoiceRecCommand(string info, VirHumanCommandType type) : base(info)
    {
        desLocalPosition = Vector3.zero;
        switch (type)
        {
            case VirHumanCommandType.initPos:
                matchPattern = "开始参观";
                desLocalPosition = new Vector3(2.99f, -0.27f, -15.45f);
                introduction = "";
                break;
            case VirHumanCommandType.shengNa:
                matchPattern = "声呐";
                desLocalPosition = new Vector3(-1f, -1f, 2.2f);
                introduction = "声呐是阿巴阿巴阿巴阿巴";
                break;

        }
        commandType = type;
    }

    public static List<VirHumanVoiceRecCommand> GetAllCommands()
    {
        List<VirHumanVoiceRecCommand> res = new List<VirHumanVoiceRecCommand>();
        res.Add(new VirHumanVoiceRecCommand("", VirHumanCommandType.initPos));
        res.Add(new VirHumanVoiceRecCommand("", VirHumanCommandType.shengNa));
        return res;
    }

    public static Dictionary<string, VirHumanVoiceRecCommand> GetDestinations()
    {
        Dictionary<string, VirHumanVoiceRecCommand> res = new Dictionary<string, VirHumanVoiceRecCommand>();
        List<VirHumanVoiceRecCommand> commands = GetAllCommands();
        foreach(var com in commands)
        {
            if (com.desLocalPosition != Vector3.zero)
            {
                res[com.matchPattern] = com;
            }
        }
        return res;
    }

    public enum VirHumanCommandType
    {
        initPos,
        shengNa
    }
}

public class SceneVoiceRecCommand : VoiceRecCommand
{
    public SceneCommandType commandType;
    public Vector3 desLocalPosition = Vector3.zero;
    public SceneVoiceRecCommand(string info, SceneCommandType type) : base(info)
    {
        switch (type)
        {
            case SceneCommandType.hideScene:
                matchPattern = "隐藏模型"; break;
            case SceneCommandType.showScene:
                matchPattern = "显示模型"; break;
            case SceneCommandType.hideDropDown:
                matchPattern = "隐藏位置"; break;
            case SceneCommandType.showDropDown:
                matchPattern = "显示位置"; break;
            case SceneCommandType.screen:
                matchPattern = "播放视频";
                desLocalPosition = new Vector3(3.205f, 0.55f, -2.49f);
                break;
            case SceneCommandType.sonar:
                matchPattern = "声呐展示"; break;
        }
        commandType = type;
    }

    public static List<SceneVoiceRecCommand> GetAllCommands()
    {
        List<SceneVoiceRecCommand> res = new List<SceneVoiceRecCommand>();
        res.Add(new SceneVoiceRecCommand("", SceneCommandType.hideScene));
        res.Add(new SceneVoiceRecCommand("", SceneCommandType.showScene));
        res.Add(new SceneVoiceRecCommand("", SceneCommandType.hideDropDown));
        res.Add(new SceneVoiceRecCommand("", SceneCommandType.showDropDown));
        res.Add(new SceneVoiceRecCommand("", SceneCommandType.screen));
        res.Add(new SceneVoiceRecCommand("", SceneCommandType.sonar));
        return res;
    }

    public enum SceneCommandType
    {
        hideScene,
        showScene,
        hideDropDown,
        showDropDown,
        screen,
        sonar
    }
}

/// <summary>
/// 飞机“出现”和“展示”相关的命令
/// </summary>
public class PlaneRelatedCommand : VoiceRecCommand
{
    public PlaneRelatedCommandType _commandType;

    public PlaneRelatedCommand(string info, PlaneRelatedCommandType type) : base(info)
    {
        switch(type)
        {
            //case PlaneRelatedCommandType.showPlane:
            //    base.matchPattern = "飞机出现";
            //    break;
            case PlaneRelatedCommandType.explodePlane:
                base.matchPattern = "机身展开";
                break;
            //case PlaneRelatedCommandType.explodeMid:
            //    base.matchPattern = "二级爆炸";
            //    break;
            case PlaneRelatedCommandType.explodeBody:
                base.matchPattern = "机身二次展开";
                break;
            //case PlaneRelatedCommandType.explodeWingLeft:
            //    base.matchPattern = "左翅膀爆炸";
            //    break;
            //case PlaneRelatedCommandType.explodeWingRight:
            //    base.matchPattern = "右翅膀爆炸";
            //    break;
            case PlaneRelatedCommandType.explodWing:
                base.matchPattern = "侧翼爆炸";
                break;
            //case PlaneRelatedCommandType.debug:
            //    base.matchPattern = "调试飞机";
            //    break;
            default:
                string logInfo = "[tcluan Debug] PlaneRelatedCommand 有未初始化的命令类型";
                Debug.Log(logInfo);
                break;
        }
        _commandType = type;
    }

    public static List<PlaneRelatedCommand> GetAllCommands()
    {
        var res = new List<PlaneRelatedCommand>
        {
            //new PlaneRelatedCommand("", PlaneRelatedCommandType.showPlane),
            new PlaneRelatedCommand("", PlaneRelatedCommandType.explodePlane),
            //new PlaneRelatedCommand("", PlaneRelatedCommandType.explodeMid),
            new PlaneRelatedCommand("", PlaneRelatedCommandType.explodeBody),
            //new PlaneRelatedCommand("", PlaneRelatedCommandType.explodeWingLeft),
            //new PlaneRelatedCommand("", PlaneRelatedCommandType.explodeWingRight)
            new PlaneRelatedCommand("", PlaneRelatedCommandType.explodWing),
            //new PlaneRelatedCommand("", PlaneRelatedCommandType.debug)
        };
        return res;
    }

    public enum PlaneRelatedCommandType
    {
        //showPlane,
        explodePlane,
        //explodeMid,
        explodeBody,
        //explodeWingLeft,
        //explodeWingRight
        explodWing,
        //debug
    }
}