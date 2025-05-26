using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Pathfinding;
using System.Diagnostics;
using static VirHumanVoiceRecCommand;
using static UnityEngine.EventSystems.EventTrigger;
using Pathfinding.Util;

public class SMPLController : MonoBehaviour
{
    //Audio
    private Dictionary<string, VirHumanVoiceRecCommand> desCommand = new Dictionary<string, VirHumanVoiceRecCommand>
    {
        {"ShengNa", new VirHumanVoiceRecCommand("", VirHumanCommandType.shengNa) }
    };
    private GameObject scene;
    private Animator walkAnim;
    private Animator talkAnim;
    private GameObject target;
    private GameObject initPos;
    private GameObject graphCenter;

    public GameObject destination;
    public GameObject walkingModel;
    public GameObject talkingModel;

    private AnimatorStateInfo animState;

    //�����������л�ģ��
    private bool isWalking; //�������Ƿ�������״̬
    private bool isInitPos; //���������ó�ʼλ��
    private Quaternion desRotation;

    private bool hasRemind = false;
    private bool hasSpoken = false;
    private string toSpeak = "";

    public Dropdown dropDown;
    public Camera arCamera;

    public Material occlusionMaterial;
    public Material texturedMaterial;
    private List<Material> materials;

    private static Vector3 consPos;

    // To Storage Target Position
    private Vector3 tempTarget;
    private Vector3 finalTarget;

    // The diastance whether virtualHuman wallking
    private float minDistance = 0.5f;
    private float maxDistance = 10.0f;

    public static void SetConsPos(Vector3 pos)
    {
        consPos = pos;
    }


    public void InitilizeObjectWithTag()
    {
        scene = GameObject.FindGameObjectWithTag("Mesh");
        target = GameObject.FindGameObjectWithTag("Target");
        initPos = GameObject.FindGameObjectWithTag("initPos");
        graphCenter = GameObject.FindGameObjectWithTag("GraphCenter");
    }

    public void SetDestination(string desName)
    {
        target.transform.localPosition = desCommand[desName].desLocalPosition;
        destination.transform.position = target.transform.position;
    }

    // Start is called before the first frame update
    void Start()
    {
        InitilizeObjectWithTag();

        walkAnim = walkingModel.GetComponent<Animator>();
        talkAnim = talkingModel.GetComponent<Animator>();

        dropDown.value = 4;
        hasSpoken = false;
        isWalking = false;
        isInitPos = true;
    }

    // Update is called once per frame
    void Update()
    {
        //destination.transform.position = despositions[dropDown.options[dropDown.value].text];
        if (walkingModel.activeSelf)
        {
            animState = walkAnim.GetCurrentAnimatorStateInfo(0);
        }
        if (talkingModel.activeSelf)
        {
            animState = talkAnim.GetCurrentAnimatorStateInfo(0);
        }

        //������߼�
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

        if(initPos.transform.position != destination.transform.position)
        {
            isInitPos = false;
        }

        //Ѱ·���߼�
        //isWalking, nearEnough, isInitPos����bool����
        if (!isInitPos)
        {
            if (isWalking)
            {
                if (NearEnough(destination.transform.position, walkingModel.transform.position))
                {
                    StopWalking();
                    SwitchToTalkMode();
                }
                else if(FarAway(arCamera.transform.position, walkingModel.transform.position))
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
                if (NearEnough(destination.transform.position, walkingModel.transform.position))
                {
                    LookAtMe(true);
                    if (!hasSpoken)
                    {
                        SpeechManager.SayFromStr(toSpeak);
                        //talkAnim.SetTrigger("introduce");
                        UnityEngine.Debug.Log("Msg in Update:" + toSpeak);
                        hasSpoken = true;
                    }
                }
                else if(FarAway(arCamera.transform.position, walkingModel.transform.position))
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

        UpdateGraphTransform();
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
        sceneMeshRenderer.material = occlusionMaterial;
    }

    public void ShowMeshRender()
    {
        MeshRenderer sceneMeshRenderer = scene.GetComponentInChildren<MeshRenderer>();
        sceneMeshRenderer.material = texturedMaterial;
    }

    public void InitializeSmplPosition()
    {
        InitilizeObjectWithTag();

        walkingModel.transform.position = initPos.transform.position;
        talkingModel.transform.position = initPos.transform.position;
        destination.transform.position = initPos.transform.position;

        SwitchToTalkMode();
        LookAtMe();
    }

    private void LookAtMe(bool isSmooth = false)
    {
        if (!isSmooth)
        {
            walkingModel.transform.LookAt(new Vector3(arCamera.transform.position.x, walkingModel.transform.position.y, arCamera.transform.position.z));
            CopyTransformState(walkingModel.transform, talkingModel.transform);
        }
        else
        {
            Vector3 targetPos = arCamera.transform.position - walkingModel.transform.position;
            targetPos.y = 0;
            desRotation = Quaternion.LookRotation(targetPos);
            walkingModel.transform.rotation = Quaternion.Slerp(walkingModel.transform.rotation, desRotation, 0.05f);
            talkingModel.transform.rotation = Quaternion.Slerp(talkingModel.transform.rotation, desRotation, 0.05f);
        }
    }
    private void CopyTransformState(Transform from, Transform to) // walkģ�ͺ�talkģ�͵ĳ���ͬ�������Ҫת���������0.213������Դ��ϵǰ��ʺɽ��
    {
        to.position = from.position;
        to.localRotation = from.localRotation;
    }

    // �л���walkģ��
    public void SwitchToWalkMode() 
    {
        isWalking = true;
        CopyTransformState(talkingModel.transform, walkingModel.transform); // ��talkģ�͵�λ�˸��Ƹ�walkģ��
        walkingModel.SetActive(true);
        talkingModel.SetActive(false);
    }

    private void SwitchToTalkMode(bool lookAtInSmooth = false) // �л���talkģ��
    {
        //StopWalking();

        isWalking = false;
        CopyTransformState(walkingModel.transform, talkingModel.transform); // ��walkģ�͵�λ�˸��Ƹ�talkģ��
        walkingModel.SetActive(false);
        talkingModel.SetActive(true);
        //LookAtMe(lookAtInSmooth);
    }

    private void StopWalking()
    {
        walkAnim.SetFloat("Speed", 0);
    }

    private void StartWalking(float speed)
    {
        walkAnim.SetFloat("Speed", speed);
    }

    public void SetDestination(Vector3 des)
    {
        destination.transform.position = des;
    }

    public Vector3 GetDesPosition(string name)
    {
        return desCommand[name].desLocalPosition;
    }

    public IEnumerator moveToDestination1(Vector3 des)
    {
        SwitchToWalkMode();
        destination.transform.position = des;
        StartWalking(0.7f);
        UnityEngine.Debug.Log("Msg in SMPL: ������·");
        yield return new WaitUntil(() => NearEnough(walkingModel.transform.position, destination.transform.position));
        StopWalking();
        // yield return new WaitUntil(() => animState.IsName("Base Layer.Idle"));
        //
        UnityEngine.Debug.Log("Msg in SMPL: ������������վ��״̬����");
        SwitchToTalkMode();
        LookAtMe(true);
        talkAnim.SetBool("Talk", SpeechManager.IsSpeaking);
        if (!hasSpoken)
        {
            SpeechManager.SayFromStr(toSpeak);
            UnityEngine.Debug.Log("Msg in SMPL: " + toSpeak);
            hasSpoken = true;
        }
    }

    public IEnumerator moveToDestination(VirHumanVoiceRecCommand desCmd)
    {
        SwitchToWalkMode();
        destination.transform.position = desCmd.desLocalPosition;
        StartWalking(0.7f);
        UnityEngine.Debug.Log("Msg in SMPL: ������·ȥ" + desCmd.commandType);
        yield return new WaitUntil(() => NearEnough(walkingModel.transform.position, destination.transform.position));
        StopWalking();
        // yield return new WaitUntil(() => animState.IsName("Base Layer.Idle"));
        //
        UnityEngine.Debug.Log("Msg in SMPL: ����" + desCmd.commandType);
        SwitchToTalkMode();
        LookAtMe(true);
        talkAnim.SetBool("Talk", SpeechManager.IsSpeaking);
        if (!hasSpoken)
        {
            toSpeak = desCmd.introduction;
            SpeechManager.SayFromStr(toSpeak);
            UnityEngine.Debug.Log("Msg in SMPL: " + toSpeak);
            hasSpoken = true;
        }
    }

    public void HideDropDown()
    {
        dropDown.gameObject.SetActive(false);
    }

    public void ShowDropDown()
    {
        dropDown.gameObject.SetActive(true);
    }

    public void StartToNav()
    {
        InitializeSmplPosition();
    }

    private Dictionary<string, Vector3> meshLocalPosition = new Dictionary<string, Vector3>
    {
        {"Screen", new Vector3(-1.11f, 0.47f, 4.037f) },
        {"Sonar", new Vector3(-0.2f, 0f, 3.3f) }
    };

    public GameObject videoScreen;
    public GameObject sonar;

    public void SummonScreen()
    {
        target.transform.localPosition = meshLocalPosition["Screen"];
        videoScreen.transform.position = target.transform.position;
        videoScreen.transform.rotation = target.transform.rotation;
        videoScreen.SetActive(true);
        FindObjectOfType<VideoManager>().PlayVideo("shengna");

        talkAnim.SetTrigger("introduce");
        SpeechManager.SayFromStr("在浩瀚无垠的蓝色领域,隐藏着无数未知的奥秘,若光线无法穿过这片昏暗,就让声波破水而行,汉界科技新一代 C 750 D 双屏图像声纳,重磅登场,最远可下潜至500米深度进行专业探测,实时动态成像,看见肉眼不可及之处");
    }

    public void SummonSonar()
    {
        target.transform.localPosition = meshLocalPosition["Sonar"];
        sonar.transform.position = target.transform.position;
        sonar.transform.rotation = target.transform.rotation;
        sonar.SetActive(true);

        talkAnim.SetTrigger("HandForward");
        SpeechManager.SayFromStr("各位游客，大家好！现在我们面前的这台C750D双屏图像声纳是一台先进的水下探测设备。它有两个屏幕，一个用于显示750kHz频率下的图像，适合大范围搜索；另一个显示1200kHz的图像，分辨率更高，适合近距离观察。");
    }

    private void UpdateGraphTransform()
    {

        AstarPath.active.AddWorkItem(() => {
            var graph = AstarPath.active.data.recastGraph;
            graph.forcedBoundsCenter = scene.transform.TransformPoint(consPos);
            //graph.forcedBoundsCenter = graphCenter.transform.position;
            Vector3 boundrotate = new Vector3(scene.transform.rotation.eulerAngles.x, scene.transform.rotation.eulerAngles.y, scene.transform.rotation.eulerAngles.z);
            //UnityEngine.Debug.Log($"new rotate: {scene.transform.rotation.eulerAngles} new pos: {graphCenter.transform.position}");
            graph.rotation = boundrotate;
            graph.RelocateNodes(graph.CalculateTransform());
        });
    }
}
