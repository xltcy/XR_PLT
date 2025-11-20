using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MediaManager : MonoBehaviour
{
    private GameObject target;
    private Dictionary<string, Vector3> meshLocalPosition = new Dictionary<string, Vector3>
    {
        {"Screen", new Vector3(2.75f,0.608316123f,-6.95144415f) },
        {"Sonar", new Vector3(2.63f,-0.1244f,-6.89699984f) },
        {"Curtain", new Vector3(2.84800005f,1.25395894f,-6.95909977f) },
        {"Wall",new Vector3(-0.0540000014f,4.26999998f,-6.83699989f) }
    };

    public GameObject videoScreen;
    public GameObject prefabSonar;
    public GameObject prefabCurtain;
    public GameObject prefabWall;
    public GameObject sonar;

    [Header("声呐的三部分")]
    public Transform sonarAbove;
    public Transform boardMiddle;
    public Transform sonarUnderneath;

    public void SummonSonarVideo()
    {
        target.transform.localPosition = meshLocalPosition["Screen"];
        videoScreen.transform.position = target.transform.position;
        videoScreen.transform.rotation = target.transform.rotation;
        videoScreen.SetActive(true);
        IntroduceSonar();
        FindObjectOfType<VideoManager>().PlayVideo("shengna");
        //Invoke("IntroduceSonar", 1.5f);
        Invoke("IntroduceGesture", 2.5f);

    }

    public void SummonFindingsVideo()
    {
        target.transform.localPosition = meshLocalPosition["Screen"];
        videoScreen.transform.position = target.transform.position;
        videoScreen.transform.rotation = target.transform.rotation;
        videoScreen.SetActive(true);
        IntroduceFindings();
        FindObjectOfType<VideoManager>().PlayVideo("findings");
        //Invoke("IntroduceFindings", 1.5f);
        Invoke("IntroduceGesture", 2.5f);
        Invoke("EndNarrating", 163f);
    }

    public void IntroduceSonar()
    {
        //SpeechManager.SayFromStr("声呐是一种利用声波的传播和反射完成测量距离、探测动态的水下探测装置。根据是否发射声波，可分为主动式声呐和被动式声呐两种。声呐可用于收集水下舰艇数据，也可用于探测鱼群动向，在军用和民用领域都有广泛的应用。");
        SpeechManager.SayFromStr("声呐是通过发射声波并接收目标反射的回波，分析时间延迟、强度及频率变化，计算目标的位置、距离及性质的设备，在复杂的水下环境中，水体浑浊度、光线随深度衰减、水体介质吸收和散射的特性的限制，通过光学成像获得水下理想的图像受到极大限制，因此声呐凭借其在穿透力、远距离、环境适应性方面的优势成为水下探测的主要设备，在海洋牧场智能化监测、核电站冷源水口生物监测、水下地基设施监测和城市大型输水管渠运维监测等领域发挥重要作用。");
        //SpeechManager.SayFromStr("声呐作为先进的水下探测技术，正改变着我们对海洋的认知。声呐在工作时，会向海底发射宽扇区覆盖的声波。当声波接触到海底或障碍物时，就会产生反射和散射回波信号，此时接收换能器迅速捕捉这些回波，并将其转化为数据，通过线缆快速传输到船上的数据处理系统，最终生成直观的三维影像。在海洋牧场智能化监测、核电站冷源水口生物监测、城市管网污水井监测、海洋工程、科研等领域能够发挥重要作用。" +
        //    "2025年中央进一步强调“建设海上牧场”，并将海洋牧场与生物农业、智慧农业结合，拓展全产业链。声呐在海洋牧场中扮演“水下之眼”的角色，结合AI算法和无人平台，对养殖区进行多角度观测，通过深度学习实时监测分析数据，实现鱼群数量统计、生长监测和健康监控等功能，在饲料成本优化、病害防控与鱼群成活率提升、人力与运维成本优化方面有着显著作用。" +
        //    "核电站冷源水通常用于冷却反应堆。水口的生物监测是连接核安全、生态保护与经济效益的核心纽带，为填补网兜海生物量监测的技术空缺，保障电厂的安全取水，实现了一套基于声纳传感器的智能监测系统。该系统融合图像处理技术、人工智能目标检测算法、深度学习算法，以及密度估计网络的综合使用，实现对水下生物的实时监测。当声呐探测到水母群靠近入水口时，其回波信号能清晰显示水母群的位置、数量和大小等信息。一旦达到预警阈值，系统立即发出警报。核电站工作人员收到预警后，会迅速采取相应措施，如启动防护装置或清理网兜设备，有效防止水母堵塞入水口，确保核电站的安全稳定运行。" +
        //    "在城市地下排水管网调查中，通过特定算法分析声呐回波信号，精确检测出污水井的井壁。是否存在裂缝、破损、变形等问题以及井底是否有淤积异物堆积等情况。能够快速、高效地获取污水井内部的详细信息，有助于准确判断污水井的状况制定合理的维护和修复方案." +
        //    "声呐，以其独特的工作原理，在海洋牧场、核电站、城市管网污水井检测、海底管道检测、海洋科研、海洋工程等众多领域发挥着重要作用，未来也将继续助力人类探索海洋奥秘。");
        //Invoke("SummonSonar", 1);
    }

    public void IntroduceFindings()
    {
        // 播放第二个视频，讲解研究成果
        SpeechManager.SayFromStr("网箱养殖因空间集中、管理便捷，是海洋牧场鱼类规模化养殖的主流模式，但精准掌握网箱内鱼群数量始终是养殖户与管理者的核心需求，当前人力效率低、产量估算不科学、喂养过程无依据和生长状态不透明等问题已经严重限制了海洋牧场的生产效益，声呐在海洋牧场中扮演“水下之眼”的角色，凭借其“无接触、高精度、实时性”的优势成为养殖网箱鱼群数量识别的核心设备之一，利用声呐能够实现对鱼群的实时监测，对养殖过程的生产决策、病害防控、经济效益评估至关重要。为了将声呐更好的应用于海洋牧场鱼群监测，我们实现了高效的滤波与图像增强方法，采用基于高斯混合的背景建模方法，运用帧间同步技术，通过多帧关联建立噪声统计模型自适应性去除噪声，获得更加“干净”、目标更加清晰的图像。通过形态学重建和模糊聚类方法实现了声呐图像轮廓检测，提取出声呐图像中的目标信息；同时运用人工智能方法，搭建深度学习网络，建立鱼群检测数据集，建立了高效的声呐图像目标检测与鱼群计数系统，为养殖过程日常投饵管理、病害预警、捕捞规划、养殖品控提供数据支撑，推动养殖管理从“经验驱动”向“数据驱动”升级，为海洋牧场的高质量发展注入新动能。\r\n核电是一种可靠的能源形式，可以提供稳定的电力供应，对增强国家的能源安全尤为重要。核电站工作时需要大量冷源水来导出核反应堆产生的热量，确保堆芯温度保持在安全范围内，防止设备损坏。然而因海洋生物聚集造成的冷源水口堵塞的情况时常发生，不断威胁核电站的安全运行。利用声呐可以实现对核电站冷源水口海洋生物和拦截设施的实时监测，我们通过搭建深度学习网络，实现了基于目标检测和基于人工智能的密度估计计数方法，通过融合声学探测、AI识别与智能预警技术，构建“监测-预警-响应”一体化监测系统，在海洋生物堵塞冷源水口时进行报警，为核电站的安全运行和国家能源安全保驾护航。");
    }

    public void SummonSonar()
    {
        //prefabSonar.GetComponent<WaveGenerator>().enabled = true;
        target.transform.localPosition = meshLocalPosition["Sonar"];
        prefabSonar.transform.position = target.transform.position;
        prefabSonar.transform.rotation = target.transform.rotation;
        prefabSonar.SetActive(true);
    }

    public void SummonSonarWithWave()
    {
        VoiceToGenerateWave();
        Invoke("HandForwardGesture", 2.5f);
        SpeechManager.SayFromStr("现在展示的是声呐发射三维声波的动画");
        Invoke("VoiceToStopWave", 20);
    }

    public void SummonSonarWithLabel()
    {
        //prefabSonar.SetActive(true);

        //sonarAbove.gameObject.SetActive(false);
        //sonarUnderneath.gameObject.SetActive(false);
        //boardMiddle.gameObject.SetActive(false);

        ShowSonarLabel();
        Invoke("IntroduceGesture", 2.5f);

        SpeechManager.SayFromStr("我们面前的这台C750D双屏图像声纳是一台先进的水下探测设备。它有两个屏幕，一个用于显示750kHz频率下的图像，适合大范围搜索；另一个显示1200kHz的图像，分辨率更高，适合近距离观察。");

        //HighLightAbove();
        Invoke("HighLightAbove", 10);

        
    }

    public void IntroduceGesture()
    {
        FindObjectOfType<SMPLController>().talkAnim.SetTrigger("introduce");
    }
    
    public void HandForwardGesture()
    {
        FindObjectOfType<SMPLController>().talkAnim.SetTrigger("HandForward");
    }

    public void HighLightAbove()
    {
        FindObjectOfType<HighLight>().HighlightAbove();
        Invoke("HighLightUnderneath", 5f);
    }

    public void HighLightUnderneath()
    {
        FindObjectOfType<HighLight>().HighlightUnderneath();
        Invoke("ResetSonar", 5f);
    }

    public void ResetSonar()
    {
        FindObjectOfType<HighLight>().ResetAbove();
        FindObjectOfType<HighLight>().ResetMiddle();
        FindObjectOfType<HighLight>().ResetUnderneath();
        FindObjectOfType<MeshController>().HideSonarRender();
        FindObjectOfType<Label3D>().HideLabel();
    }

    public void ShowSonarLabel()
    {
        FindObjectOfType<Label3D>().ShowLable();
    }

    public void SummonCurtain()
    {
        target.transform.localPosition = meshLocalPosition["Curtain"];
        prefabCurtain.transform.position = target.transform.position;
        prefabCurtain.transform.rotation = target.transform.rotation;
        prefabCurtain.SetActive(true);

        Invoke("SummonSonarVideo", 5);
    }
    public void SummonWall()
    {
        target.transform.localPosition = meshLocalPosition["Wall"];
        prefabWall.transform.position = target.transform.position;
        prefabWall.transform.rotation = target.transform.rotation;
        prefabWall.SetActive(true);
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

    public void VoiceToGenerateWave()
    {
        FindObjectOfType<SonarWaveManager>().StartGenerate();
    }

    public void VoiceToStopWave()
    {
        FindObjectOfType<SonarWaveManager>().StopGenerateAndDestroyWave();
    }

    public void EndNarrating()
    {
        SpeechManager.SayFromStr("您还有其他想了解的吗");
    }

    // Start is called before the first frame update
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Target");
    }

    // Update is called once per frame
    void Update()
    {
        //target = GameObject.FindGameObjectWithTag("Target");
    }
}
