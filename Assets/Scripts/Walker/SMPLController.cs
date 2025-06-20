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
        {"Curtain", new Vector3(-1.054f, 1.178f, 4.15f) }
    };

    public GameObject videoScreen;
    public GameObject prefabSonar;
    public GameObject prefabCurtain;
    public GameObject sonar;

    public void SummonScreen()
    {
        target.transform.localPosition = meshLocalPosition["Screen"];
        videoScreen.transform.position = target.transform.position;
        videoScreen.transform.rotation = target.transform.rotation;
        videoScreen.SetActive(true);
        FindObjectOfType<VideoManager>().PlayVideo("test");

        talkAnim.SetTrigger("introduce");
        SpeechManager.SayFromStr("声呐是一种利用声波的传播和反射完成测量距离、探测动态的水下探测装置。根据是否发射声波，可分为主动式声呐和被动式声呐两种。声呐可用于收集水下舰艇数据，也可用于探测鱼群动向，在军用和民用领域都有广泛的应用。");

        Invoke("SummonSonar", 20);
    }

    public void SummonSonar()
    {
        target.transform.localPosition = meshLocalPosition["Sonar"];
        prefabSonar.transform.position = target.transform.position;
        prefabSonar.transform.rotation = target.transform.rotation;
        prefabSonar.SetActive(true);

        talkAnim.SetTrigger("HandForward");
        SpeechManager.SayFromStr("现在我们面前的这台C750D双屏图像声纳是一台先进的水下探测设备。它有两个屏幕，一个用于显示750kHz频率下的图像，适合大范围搜索；另一个显示1200kHz的图像，分辨率更高，适合近距离观察。");

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
        Invoke("HideSonar", 6);
    }

    private void HideSonar()
    {
        prefabSonar.SetActive(false);
    }
}
