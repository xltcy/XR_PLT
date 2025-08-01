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
        {"Sonar", new Vector3(2.63f,-0.0520000011f,-6.89699984f) },
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
        FindObjectOfType<VideoManager>().PlayVideo("shengna");
        Invoke("IntroduceSonar", 1.5f);
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
        Invoke("IntroduceFindings", 1.5f);
        Invoke("IntroduceGesture", 2.5f);
        Invoke("EndNarrating", 132f);
    }

    public void IntroduceSonar()
    {
        //SpeechManager.SayFromStr("声呐是一种利用声波的传播和反射完成测量距离、探测动态的水下探测装置。根据是否发射声波，可分为主动式声呐和被动式声呐两种。声呐可用于收集水下舰艇数据，也可用于探测鱼群动向，在军用和民用领域都有广泛的应用。");
        SpeechManager.SayFromStr("声纳作为先进的水下探测技术，正改变着我们对海洋的认知。 声纳在工作时，会向海底发射宽散区覆盖的声波。 当声波接触到海底或障碍物时，就会产生反射和散射回波信号，此时接收换能器迅速捕捉这些回波，并将其转化为数据，通过线缆快速传输到船上的数据处理系统，最终生成直观的三维影像。 在海洋牧场智能化监测、核电站冷源水口生物监测、 城市管网污水井检测、海洋工程、科研等领域能够发挥重要作用。");
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
        SpeechManager.SayFromStr("为了将声纳更好的应用于水下监测，我们在声纳图像域处理、 声纳图像目标检测与数量估计方面开展了研究并取得了阶段性成果。 在声纳图像域处理方面，实现了高效的滤波与图像增强方法，采用基于高斯混合的背景建模方法以及针尖同步技术。 通过多帧关联创建噪声统计模型自适应性去除噪声，获得更加干净、目标更加清晰的图像。 在声纳图像目标检测方面，通过形态学重建和模糊剧类方法实现了声纳图像轮廓检测，提取出声纳图像中的目标信息，同时利用人工智能方法搭建深度学习网络，创建一群检测数据集，实现了高效的声纳图像目标检测方法。 在数量估计方面，实现了基于目标检测的技术、 运动轨迹技术和基于AI的密度估计多种技术方法。 基于研究成果，我们设计了基于声纳的水下生物监测系统并开展示范应用。 在海洋牧场方面，声纳扮演水下之眼的角色，通过监测系统可以实现对养植物的数量统计、 生长监测和健康监控等功能，对当前海产养殖人力效率低、产量估算不科学、 喂养过程无依据和生长状态不透明等问题提供了新途径。 在核电站EV方面，监测系统能够有效地对核电站冷源水口生物进行实时监测，通过融合声学探测、AI识别与智能预警技术构建监测预警响应一体化体系。 在海洋生物堵塞冷源水口时进行报警，从而减少重大安全问题的出现。");
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
        Invoke("IntroduceGesture", 2.5f);
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
