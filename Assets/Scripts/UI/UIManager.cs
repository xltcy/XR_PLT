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
    public GameObject LoadingView;
    public GameObject smplController;

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
        var sceneData = FindObjectOfType<SceneController>()?.sceneData;
        if (sceneData == null)
        {
            // Error
            Debug.Log("No vailable explaination point!!!");
            return;
        }
        if (!initPos)
        {
            smplController.SetActive(true);
            FindObjectOfType<SMPLController>().InitializeSmplPosition();
            initPos = true;
        }
        if (sceneData.explanationPoints.Count == 1)
        {
            // skip selectDestination.
            TransToVirtualManIntroUI(sceneData.explanationPoints[0].id);
            return;
        }
        // Trans to select destination.
        SwitchRunState(RunState.SelectDestination);
    }

    public void TransToVirtualManIntroUI(string pointId)
    {
        FindObjectOfType<SceneController>().SetSelectedExplainationPoint(pointId);
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
        TransToVirtualManIntroUI(item.pointId);
    }

    private void RequestLoadingStatus(bool setStart)
    {
        LoadingView.SetActive(setStart);
    }

    public static void SetLoadingStatus(bool setStart)
    {
        FindObjectOfType<UIManager>().RequestLoadingStatus(setStart);
    }

    public enum RunState
    {
        Start,
        SelectDestination,
        VirtualManIntro
    }
}
