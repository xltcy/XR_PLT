using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour, SelectDesController.SelectDesActionInterface
{
    public GameObject StartView;
    public GameObject SelectDesView;
    public GameObject VirtualManIntroView;
    public GameObject smplController;
    public GameObject mediaManager;

    // Initialize VirtualMan For First Time
    private bool initPos = false;

    private RunState curState = RunState.Start;
    // Start is called before the first frame update
    void Start()
    {
        TransToStartUI();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TransToStartUI()
    {
        SwitchRunState(RunState.Start);
    }

    public void TransToSelectDesUI()
    {
        SwitchRunState(RunState.SelectDestination);

        if(!initPos)
        {
            smplController.SetActive(true);
            FindObjectOfType<SMPLController>().InitializeSmplPosition();
            initPos = true;
        }

    }

    public void TansToVirtualManIntroUI()
    {
        SwitchRunState(RunState.VirtualManIntro);
    }

    private void SwitchRunState(RunState newState)
    {
        SwitchForStart(newState);
        SwitchForSelectDes(newState);
        SwitchForVirtualManIntro(newState);
        curState = newState;
        Debug.Log($"UIManager SwitchRunState{newState}");
    }

    public void ShowManager()
    {
        if (!initPos)
        {
            smplController.SetActive(true);
            mediaManager.SetActive(true);
            //FindObjectOfType<SMPLController>().InitializeSmplPosition();
            initPos = true;
        }
    }

    public void SkipSelect()
    {
        SwitchRunState(RunState.VirtualManIntro);
        //ShowManager();
        //if (!initPos)
        //{
        //    smplController.SetActive(true);
        //    mediaManager.SetActive(true);
        //    FindObjectOfType<SMPLController>().InitializeSmplPosition();
        //    initPos = true;
        //}
        FindObjectOfType<SMPLController>().InitializeSmplPosition();
        SpeechManager.SayFromStr("欢迎来到VR实验室科教平台，接下来我将带你参观介绍水下装备数字孪生底座系统，请跟我来");
        Invoke("GotoShengNa", 12);
    }

    private void SwitchForStart(RunState newState)
    {
        bool setActive = newState == RunState.Start;
        StartView.SetActive(setActive);
    }

    private void SwitchForSelectDes(RunState newState)
    {
        bool setActive = newState == RunState.SelectDestination;
        SelectDesView.SetActive(setActive);
        if (setActive)
        {
            SelectDesController selectDesCtrl = FindObjectOfType<SelectDesController>();
            selectDesCtrl.InitState();
            selectDesCtrl.selectDesInterface = this;
        }
    }

    private void SwitchForVirtualManIntro(RunState newState)
    {
        bool setActive = newState == RunState.VirtualManIntro;
        VirtualManIntroView.SetActive(setActive);
    }

    void SelectDesController.SelectDesActionInterface.OnSelectDesAt(SelectDesModel item)
    {
        // todo set new des & trans UI run state.
        Debug.Log($"UIManager SelectDesActionInterface OnSelectDesAt {item.title}");
        TansToVirtualManIntroUI();
        FindObjectOfType<SMPLController>().SetDestination("ShengNa"); 
    }

    public enum RunState
    {
        Start,
        SelectDestination,
        VirtualManIntro
    }

    public void GotoShengNa()
    {
        FindObjectOfType<SMPLController>().SetDestination("ShengNa");
    }
}
