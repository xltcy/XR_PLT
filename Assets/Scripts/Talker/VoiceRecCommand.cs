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
                matchPattern = "��ʼʶ��(?!������)";break;
            case IRVCommandType.TRACK_DISABLE:
                matchPattern = "�ر�ʶ��";break;
            case IRVCommandType.VIDEO_PLAY:
                matchPattern = "��ʼ����"; break;
            case IRVCommandType.VIDEO_PAUSE:
                matchPattern = "��ͣ����"; break;
            case IRVCommandType.VIDEO_STOP:
                matchPattern = "ֹͣ����"; break;
            case IRVCommandType.TRACK_ENABLE_AUTO_PLAY_VIDEO:
                matchPattern = "��ʼʶ�𲢲���"; break;
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
                matchPattern = "��ʼ�ι�";
                desLocalPosition = new Vector3(2.99f, -0.27f, -15.45f);
                introduction = "";
                break;
            case VirHumanCommandType.shengNa:
                matchPattern = "����";
                desLocalPosition = new Vector3(-1f, -1f, 2.2f);
                introduction = "�����ǰ��Ͱ��Ͱ��Ͱ���";
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
                matchPattern = "����ģ��"; break;
            case SceneCommandType.showScene:
                matchPattern = "��ʾģ��"; break;
            case SceneCommandType.hideDropDown:
                matchPattern = "����λ��"; break;
            case SceneCommandType.showDropDown:
                matchPattern = "��ʾλ��"; break;
            case SceneCommandType.screen:
                matchPattern = "������Ƶ";
                desLocalPosition = new Vector3(3.205f, 0.55f, -2.49f);
                break;
            case SceneCommandType.sonar:
                matchPattern = "����չʾ"; break;
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
/// �ɻ������֡��͡�չʾ����ص�����
/// </summary>
public class PlaneRelatedCommand : VoiceRecCommand
{
    public PlaneRelatedCommandType _commandType;

    public PlaneRelatedCommand(string info, PlaneRelatedCommandType type) : base(info)
    {
        switch(type)
        {
            //case PlaneRelatedCommandType.showPlane:
            //    base.matchPattern = "�ɻ�����";
            //    break;
            case PlaneRelatedCommandType.explodePlane:
                base.matchPattern = "����չ��";
                break;
            //case PlaneRelatedCommandType.explodeMid:
            //    base.matchPattern = "������ը";
            //    break;
            case PlaneRelatedCommandType.explodeBody:
                base.matchPattern = "�������չ��";
                break;
            //case PlaneRelatedCommandType.explodeWingLeft:
            //    base.matchPattern = "����ը";
            //    break;
            //case PlaneRelatedCommandType.explodeWingRight:
            //    base.matchPattern = "�ҳ��ը";
            //    break;
            case PlaneRelatedCommandType.explodWing:
                base.matchPattern = "����ը";
                break;
            //case PlaneRelatedCommandType.debug:
            //    base.matchPattern = "���Էɻ�";
            //    break;
            default:
                string logInfo = "[tcluan Debug] PlaneRelatedCommand ��δ��ʼ������������";
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