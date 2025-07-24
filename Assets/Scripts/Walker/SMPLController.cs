using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static VirHumanVoiceRecCommand;

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
    private GameObject initPos;

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
    private string arriveIntroduction = "";

    public GameObject buttons;
    public Camera arCamera;

    public Material occlusionMaterial;
    public Material texturedMaterial;
    private List<Material> materials;

    private static Vector3 consPos;

    // The diastance whether virtualHuman wallking
    private float minDistance = 0.5f;
    private float maxDistance = 15.0f;

    public static void SetConsPos(Vector3 pos)
    {
        consPos = pos;
    }


    public void InitilizeObjectWithTag()
    {
        scene = GameObject.FindGameObjectWithTag(SceneController.GameObjectTag.Mesh.ToString());
        initPos = GameObject.FindGameObjectWithTag(SceneController.GameObjectTag.initPos.ToString());
    }

    public void SetDestination(string desName)
    {
        destination.transform.position = scene.transform.TransformPoint(desCommand[desName].desLocalPosition);
    }

    // Start is called before the first frame update
    void Start()
    {
        // InitilizeObjectWithTag();

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
        //debugText.text = $"Destination: {destination.transform.position}\nGuide:{walkingModel.transform.position}";
        if (walkingModel.activeSelf)
        {
            animState = walkAnim.GetCurrentAnimatorStateInfo(0);
        }
        if (talkingModel.activeSelf)
        {
            animState = talkAnim.GetCurrentAnimatorStateInfo(0);
        }

        if (initPos != null && destination != null)
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

        if (initPos.transform.position != destination.transform.position)
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
                else if (FarAway(arCamera.transform.position, walkingModel.transform.position))
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
                        String str = arriveIntroduction.Length == 0 ? toSpeak : arriveIntroduction;
                        SpeechManager.SayFromStr(str);
                        //talkAnim.SetTrigger("introduce");
                        UnityEngine.Debug.Log("Msg in Update:" + str);
                        hasSpoken = true;
                    }
                }
                else if (FarAway(arCamera.transform.position, walkingModel.transform.position))
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

    public void SetDestination(Vector3 des, String initialIntro = "", String arriveIntro = "")
    {
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

    /**
    * do nothing
    */
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

    public void IntroduceString(String introduction)
    {
        //talkAnim.SetTrigger("introduce");
        SpeechManager.SayFromStr(introduction);
    }

    /**
     * Set Anim for virtualMan      
     */
    public void AvatarAnim(string animTrigger)
    {
        if (!isWalking)
        {
            talkAnim.SetTrigger(animTrigger);
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
}
