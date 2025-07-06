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

    public GameObject buttons;
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
    private float maxDistance = 15.0f;

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

    public void HideButton()
    {
        buttons.gameObject.SetActive(false);
    }

    public void ShowButton()
    {
        buttons.gameObject.SetActive(true);
    }

    public void StartToNav()
    {
        InitializeSmplPosition();
    }

    private Dictionary<string, Vector3> meshLocalPosition = new Dictionary<string, Vector3>
    {
        {"Screen", new Vector3(-1.09f, 0.47f, 4.037f) },
        //{"Sonar", new Vector3(-0.2f, 0f, 3.3f) },
        {"Sonar", new Vector3(-0.9f, -0.2f, 3.9f) },
        {"Curtain", new Vector3(-1.054f, 1.178f, 4.15f) },
        {"Wall",new Vector3(1.6229f,0.3723f,2.8281f) }
    };

    public GameObject videoScreen;
    public GameObject prefabSonar;
    public GameObject prefabCurtain;
    public GameObject prefabWall;
    public GameObject sonar;

    public void SummonScreen()
    {
        target.transform.localPosition = meshLocalPosition["Screen"];
        videoScreen.transform.position = target.transform.position;
        videoScreen.transform.rotation = target.transform.rotation;
        videoScreen.SetActive(true);
        FindObjectOfType<VideoManager>().PlayVideo("test");
        Invoke("Introduce", 1.9f);
        
    }

    public void Introduce()
    {
        talkAnim.SetTrigger("introduce");
        //SpeechManager.SayFromStr("声呐是一种利用声波的传播和反射完成测量距离、探测动态的水下探测装置。根据是否发射声波，可分为主动式声呐和被动式声呐两种。声呐可用于收集水下舰艇数据，也可用于探测鱼群动向，在军用和民用领域都有广泛的应用。");
        SpeechManager.SayFromStr("声呐作为先进的水下探测技术，正改变着我们对海洋的认知。声呐在工作时，会向海底发射宽扇区覆盖的声波。当声波接触到海底或障碍物时，就会产生反射和散射回波信号，此时接收换能器迅速捕捉这些回波，并将其转化为数据，通过线缆快速传输到船上的数据处理系统，最终生成直观的三维影像。在海洋牧场智能化监测、核电站冷源水口生物监测、城市管网污水井监测、海洋工程、科研等领域能够发挥重要作用。" +
            "2025年中央进一步强调“建设海上牧场”，并将海洋牧场与生物农业、智慧农业结合，拓展全产业链。声呐在海洋牧场中扮演“水下之眼”的角色，结合AI算法和无人平台，对养殖区进行多角度观测，通过深度学习实时监测分析数据，实现鱼群数量统计、生长监测和健康监控等功能，在饲料成本优化、病害防控与鱼群成活率提升、人力与运维成本优化方面有着显著作用。" +
            "核电站冷源水通常用于冷却反应堆。水口的生物监测是连接核安全、生态保护与经济效益的核心纽带，为填补网兜海生物量监测的技术空缺，保障电厂的安全取水，实现了一套基于声纳传感器的智能监测系统。该系统融合图像处理技术、人工智能目标检测算法、深度学习算法，以及密度估计网络的综合使用，实现对水下生物的实时监测。当声呐探测到水母群靠近入水口时，其回波信号能清晰显示水母群的位置、数量和大小等信息。一旦达到预警阈值，系统立即发出警报。核电站工作人员收到预警后，会迅速采取相应措施，如启动防护装置或清理网兜设备，有效防止水母堵塞入水口，确保核电站的安全稳定运行。" +
            "在城市地下排水管网调查中，通过特定算法分析声呐回波信号，精确检测出污水井的井壁。是否存在裂缝、破损、变形等问题以及井底是否有淤积异物堆积等情况。能够快速、高效地获取污水井内部的详细信息，有助于准确判断污水井的状况制定合理的维护和修复方案." +
            "声呐，以其独特的工作原理，在海洋牧场、核电站、城市管网污水井检测、海底管道检测、海洋科研、海洋工程等众多领域发挥着重要作用，未来也将继续助力人类探索海洋奥秘。");
        Invoke("SummonSonar", 1);
    }

    public void SummonSonar()
    {
        target.transform.localPosition = meshLocalPosition["Sonar"];
        prefabSonar.transform.position = target.transform.position;
        prefabSonar.transform.rotation = target.transform.rotation;
        prefabSonar.SetActive(true);

        talkAnim.SetTrigger("HandForward");
        //SpeechManager.SayFromStr("现在我们面前的这台C750D双屏图像声纳是一台先进的水下探测设备。它有两个屏幕，一个用于显示750kHz频率下的图像，适合大范围搜索；另一个显示1200kHz的图像，分辨率更高，适合近距离观察。");

        Invoke("SeparateSonar", 15);
    }

    public void SummonCurtain()
    {
        target.transform.localPosition = meshLocalPosition["Curtain"];
        prefabCurtain.transform.position = target.transform.position;
        prefabCurtain.transform.rotation = target.transform.rotation;
        prefabCurtain.SetActive(true);

        Invoke("SummonScreen", 5);
    }
    public void SummonWall()
    {
        target.transform.localPosition = meshLocalPosition["Wall"];
        prefabWall.transform.position = target.transform.position;
        prefabWall.transform.rotation = target.transform.rotation;
        prefabWall.SetActive(true);
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

    public void SeparateSonar()
    {
        ModelTreeNode.OneDofExplosion(sonar);
        Invoke("RecoverSonar", 6);
    }

    public void RecoverSonar()
    {
        ModelTreeNode.OneDofRecovery(sonar);
        Invoke("HideSonar", 12);
    }

    private void HideSonar()
    {
        prefabSonar.SetActive(false);
    }
}
