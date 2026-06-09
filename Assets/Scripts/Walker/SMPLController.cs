using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Pathfinding;
using TickSystem;
using static VirHumanVoiceRecCommand;

public class SMPLController : BaseController, ITickerUpdate
{
    private string DefaultAvatarModelName = "guide_qs";
    public string SelectedAvatarName = string.Empty;
    
    //Audio
    private Dictionary<string, VirHumanVoiceRecCommand> desCommand = new Dictionary<string, VirHumanVoiceRecCommand>
    {
        {"ShengNa", new VirHumanVoiceRecCommand("", VirHumanCommandType.shengNa) }
    };
    private GameObject scene;
    private Animator avatarAnimator;

    public Transform avatarRoot;
    public GameObject destination;
    public GameObject avatarModel;

    private AnimatorStateInfo animState;

    //控制角色移动的模型
    private bool isWalking; //判断角色是否在移动状态
    private bool isInitPos; //检查是否到达初始位置
    private Quaternion desRotation;

    private bool hasRemind = false;
    private bool hasSpoken = false;
    private string toSpeak = "";
    private string arriveIntroduction = "";

    public GameObject buttons;
    public Camera arCamera;

    public Material occlusionMaterial;
    public Material texturedMaterial;
    private List<Material> materials;

    private static Vector3 consPos;

    // The diastance whether virtualHuman wallking
    private float minDistance = 0.1f;
    private float maxDistance = 36.0f; // TODO 原15f，工训楼介绍临时修改为36

    public override void OnRegister()
    {
        base.OnRegister();
        Init();
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
        SetControllerTickActive(false);
    }

    public static void SetConsPos(Vector3 pos)
    {
        consPos = pos;
    }


    public void InitilizeObjectWithTag()
    {
        scene = ControllerRefer.SceneController.Scene;
    }

    private Vector3 initPos
    {
        get
        {
            var pos = ControllerRefer.SceneController.SceneData?.initPosition ?? Vector3.zero;
            return scene.transform.TransformPoint(pos);
        }
    }

    public void SetDestination(string desName)
    {
        destination.transform.position = scene.transform.TransformPoint(desCommand[desName].desLocalPosition);
    }

    // Start is called before the first frame update
    void Init()
    {
        hasSpoken = false;
        isWalking = false;
        isInitPos = true;
    }

    /// <summary>
    /// 设置模型的激活状态，同时注册或注销Tick更新。
    /// 与 StopWalking()功能不一样，注意区别
    /// </summary>
    /// <param name="isActive"></param>
    public void SetControllerTickActive(bool isActive)
    {
        avatarModel.SetVisible(isActive);
        
        if (isActive)
        {
            TickController.RegisterTick(this);
        }
        else
        {
            TickController.UnRegisterTick(this);
        }
    }

    public void CreateSelectedModel()
    {
        CreateModel(SelectedAvatarName);
    }
    
    public void CreateModel(string avatarModelName = "")
    {
        // 如果avatarModelName为空且avatarModel已经存在，则不重新创建模型
        if (avatarModelName.IsNullOrEmpty() && avatarModel)
        {
            return;
        }
        
        avatarModelName = avatarModelName == string.Empty ? DefaultAvatarModelName : avatarModelName;
        
        ManagerRefer.GameObjectPoolManager.Recycle(avatarModel);
        avatarModel = ManagerRefer.GameObjectPoolManager.Instantiate($"Prefab/Avatar/{avatarModelName}", avatarRoot);
        
        
        if (avatarModel)
        {
            // 设置寻路目标

            var aiDestinationSetter = avatarModel.transform.GetComponent<AIDestinationSetter>();
            if (aiDestinationSetter)
            {
                aiDestinationSetter.target = destination.transform;
            }

            // 设置语音驱动头部
            var head = avatarModel.transform.FindDeep("Head_Mesh");
            if (!head)
            {
                head = avatarModel.transform.FindDeep("AvatarHead");
                if (ControllerRefer.SpeechManager.speech2BlendshapeController != null)
                {
                    ControllerRefer.SpeechManager.speech2BlendshapeController.SetGuideHead(head.gameObject);
                }
            }
        }
        
        avatarAnimator = avatarModel.GetComponent<Animator>();
        
        //停止运动
        StopWalking();
        SwitchToTalkMode();
    }

    // Tick is called once per frame
    public void Tick()
    {
        //destination.transform.position = despositions[dropDown.options[dropDown.value].text];
        //debugText.text = $"Destination: {destination.transform.position}\nGuide:{avatarModel.transform.position}";
        if (avatarModel && avatarModel.activeSelf)
        {
            animState = avatarAnimator.GetCurrentAnimatorStateInfo(0);
        }

        if (destination != null)
        {
            WalkCheck();
        }
        
        if (scene != null)
        {
            UpdateGraphTransform();
        }
    }

    private void WalkCheck()
    {
        //行走检查逻辑
        if (!isInitPos)
        {
            foreach (var cmd in desCommand)
            {
                if (NearEnough(destination.transform.position, cmd.Value.desLocalPosition))
                {
                    toSpeak = cmd.Value.introduction;
                }
            }
        }

        if (initPos != destination.transform.position)
        {
            isInitPos = false;
        }

        //寻路逻辑
        //isWalking, nearEnough, isInitPos都是bool类型
        if (!isInitPos)
        {
            if (isWalking)
            {
                if (NearEnough(destination.transform.position, avatarModel.transform.position))
                {
                    StopWalking();
                    SwitchToTalkMode();
                }
                else if (FarAway(arCamera.transform.position, avatarModel.transform.position))
                {
                    StopWalking();
                    SwitchToTalkMode();
                }
                else
                {
                    StartWalking(0.7f);
                }
            }
            else
            {
                if (NearEnough(destination.transform.position, avatarModel.transform.position))
                {
                    LookAtMe(true);
                    if (!hasSpoken)
                    {
                        String str = arriveIntroduction.Length == 0 ? toSpeak : arriveIntroduction;
                        SpeechManager.SayFromStr(str);
                        //avatarAnimator.SetTrigger("introduce");
                        UnityEngine.Debug.Log("Msg in Update:" + str);
                        hasSpoken = true;
                    }
                }
                else if (FarAway(arCamera.transform.position, avatarModel.transform.position))
                {
                    //SwitchToWalkMode();
                    LookAtMe(true);
                    if (!hasRemind)
                    {
                        SpeechManager.SayFromStr("请跟上我");
                        hasRemind = true;
                    }
                }
                else
                {
                    SwitchToWalkMode();
                    hasSpoken = false;
                }
            }
        }
    }

    bool NearEnough(Vector3 a, Vector3 b)
    {
        //Debug.Log((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
        return (a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z) < minDistance;

    }

    bool FarAway(Vector3 a, Vector3 b)
    {
        //Debug.Log((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
        return (a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z) > maxDistance;

    }


    public void HideMeshRender()
    {
        MeshRenderer sceneMeshRenderer = scene.GetComponentInChildren<MeshRenderer>();
        //sceneMeshRenderer.material = occlusionMaterial;
        Material[] newMaterials = new Material[2];  // 假设你要设置两个材质

        // 给每个材质槽赋值
        newMaterials[0] = occlusionMaterial;
        newMaterials[1] = occlusionMaterial;

        // 设置到 MeshRenderer 上
        GetComponent<Renderer>().materials = newMaterials;

    }

    public void ShowMeshRender()
    {
        MeshRenderer sceneMeshRenderer = scene.GetComponentInChildren<MeshRenderer>();
        sceneMeshRenderer.material = texturedMaterial;
    }

    public void InitializeSmplPosition()
    {
        InitilizeObjectWithTag();

        StopWalking();
        SwitchToTalkMode();
        
        avatarModel.transform.position = initPos;
        destination.transform.position = initPos;
        
        LookAtMe();
    }

    private void LookAtMe(bool isSmooth = false)
    {
        if (!isSmooth)
        {
            avatarModel.transform.LookAt(new Vector3(arCamera.transform.position.x, avatarModel.transform.position.y, arCamera.transform.position.z));
        }
        else
        {
            Vector3 targetPos = arCamera.transform.position - avatarModel.transform.position;
            targetPos.y = 0;
            desRotation = Quaternion.LookRotation(targetPos);
            avatarModel.transform.rotation = Quaternion.Slerp(avatarModel.transform.rotation, desRotation, 0.05f);
        }
    }
    private void CopyTransformState(Transform from, Transform to) // walk模型和talk模型的比例不同，需要转换坐标，乘以0.213比例系数，确保两个模型位置同步
    {
        to.position = from.position;
        to.localRotation = from.localRotation;
    }

    // 切换到Walk模式
    public void SwitchToWalkMode() 
    {
        isWalking = true;
    }

    private void SwitchToTalkMode(bool lookAtInSmooth = false) // 切换到talk模式
    {
        isWalking = false;
        //LookAtMe(lookAtInSmooth);
    }

    /// <summary>
    /// 虚拟人走路动画停止，RichAI停止移动
    /// </summary>
    private void StopWalking()
    {
        avatarAnimator?.SetFloat("Speed", 0);
        if (avatarModel)
        {
            var richAI = avatarModel.GetComponent<RichAI>();
            if (richAI)
            {
                richAI.enabled = false;
            }
        }
    }

    /// <summary>
    /// 虚拟人走路动画停止，RichAI开始移动
    /// </summary>
    private void StartWalking(float speed)
    {
        avatarAnimator?.SetFloat("Speed", speed);
        if (avatarModel)
        {
            var richAI = avatarModel.GetComponent<RichAI>();
            if (richAI)
            {
                richAI.enabled = true;
            }
        }
    }

    public void SetDestination(Vector3 des, String initialIntro = "", String arriveIntro = "")
    {
        if (!destination || !scene)
        {
            return;
        }
        arriveIntroduction = arriveIntro;
        SpeechManager.SayFromStr(initialIntro, onSpeakComplete: () => {
            MainThreadDispatcher.InvokeOnMainThread(() =>
            {
                // Work in main thread.
                destination.transform.position = scene.transform.TransformPoint(des);
            });
        });
    }

    /**
    * do nothing
    */
    public Vector3 GetDesPosition(string name)
    {
        return desCommand[name].desLocalPosition;
    }

    /**
    * do nothing
    */
    public IEnumerator moveToDestination1(Vector3 des)
    {
        SwitchToWalkMode();
        destination.transform.position = des;
        StartWalking(0.7f);
        UnityEngine.Debug.Log("Msg in SMPL: 开始寻路");
        yield return new WaitUntil(() => NearEnough(avatarModel.transform.position, destination.transform.position));
        StopWalking();
        // yield return new WaitUntil(() => animState.IsName("Base Layer.Idle"));
        //
        UnityEngine.Debug.Log("Msg in SMPL: 已经到达并进入站立状态");
        SwitchToTalkMode();
        LookAtMe(true);
        avatarAnimator.SetBool("Talk", SpeechManager.IsSpeaking);
        if (!hasSpoken)
        {
            SpeechManager.SayFromStr(toSpeak);
            UnityEngine.Debug.Log("Msg in SMPL: " + toSpeak);
            hasSpoken = true;
        }
    }

    /**
    * do nothing
    */
    public IEnumerator moveToDestination(VirHumanVoiceRecCommand desCmd)
    {
        SwitchToWalkMode();
        destination.transform.position = desCmd.desLocalPosition;
        StartWalking(0.7f);
        UnityEngine.Debug.Log("Msg in SMPL: 开始寻路去" + desCmd.commandType);
        yield return new WaitUntil(() => NearEnough(avatarModel.transform.position, destination.transform.position));
        StopWalking();
        // yield return new WaitUntil(() => animState.IsName("Base Layer.Idle"));
        //
        UnityEngine.Debug.Log("Msg in SMPL: 到达" + desCmd.commandType);
        SwitchToTalkMode();
        LookAtMe(true);
        avatarAnimator.SetBool("Talk", SpeechManager.IsSpeaking);
        if (!hasSpoken)
        {
            toSpeak = desCmd.introduction;
            SpeechManager.SayFromStr(toSpeak);
            UnityEngine.Debug.Log("Msg in SMPL: " + toSpeak);
            hasSpoken = true;
        }
    }

    public void HideButton()
    {
        buttons.gameObject.SetActive(false);
    }

    public void ShowButton()
    {
        buttons.gameObject.SetActive(true);
    }

    /**
    * do nothing
    */
    public void StartToNav()
    {
        InitializeSmplPosition();
    }

    private Dictionary<string, Vector3> meshLocalPosition = new Dictionary<string, Vector3>
    {
        //{"Sonar", new Vector3(-0.2f, 0f, 3.3f) },
        {"Sonar", new Vector3(-0.9f, -0.2f, 3.9f) }
    };

    public void IntroduceString(String introduction, Action onComplete = null)
    {
        //avatarAnimator.SetTrigger("introduce");
        SpeechManager.SayFromStr(introduction, onComplete);
    }

    /**
     * Set Anim for virtualMan      
     */
    public void AvatarAnim(string animTrigger)
    {
        if (!isWalking)
        {
            avatarAnimator.SetTrigger(animTrigger);
        }
    }

    /**
     * Keep Graph with scene's transform.
     */
    private void UpdateGraphTransform()
    {

        AstarPath.active.AddWorkItem(() => {
            var graph = AstarPath.active.data.recastGraph;
            graph.forcedBoundsCenter = scene.transform.TransformPoint(consPos);
            Vector3 boundrotate = new Vector3(scene.transform.rotation.eulerAngles.x, scene.transform.rotation.eulerAngles.y, scene.transform.rotation.eulerAngles.z);
            graph.rotation = boundrotate;
            graph.RelocateNodes(graph.CalculateTransform());
        });
    }

    class PositionParam
    {
        public float x;
        public float y;
        public float z;
        public string initialIntro;
        public string arriveIntro;
        public Vector3 pos => new Vector3(x, y, z);
    }

    public void JsonSetDestination(bool isStartAction, string paramJson)
    {
        var param = JsonUtility.FromJson<PositionParam>(paramJson);

        SetDestination(param.pos, param.initialIntro, param.arriveIntro);
    }
}
