using System;
using Newtonsoft.Json;
using UniGLTF;
using UnityEngine;

public class DynamicEventObject : DynamicObject
{
    [SerializeField]
    public string EventObjectID;
    
    protected override void Start()
    {
        base.Start();
        
        this.AddEventListener(EventConstant.HIGHLIGHT_OBJECT, OnHighlightObjectEvent);
        this.AddEventListener(EventConstant.GENERATE_WAVE, OnGenerateWaveEvent);
    }

    protected override void OnDestroy()
    {
        this.RemoveEventListener(EventConstant.HIGHLIGHT_OBJECT, OnHighlightObjectEvent);
        this.RemoveEventListener(EventConstant.GENERATE_WAVE, OnGenerateWaveEvent);
        
        base.OnDestroy();
    }
    
    
    private void OnHighlightObjectEvent(EventData param)
    {
        //提取事件数据
        var data = param.GetData<SceneController.ProgramEventData>();

        //提取自定义数据
        var highLightEventData = data?.actionData?.eventData as ProgramEvent.HighlightNodeEventParam;
        if (highLightEventData == null) return;

        //筛选ID
        if (EventObjectID != highLightEventData.EventObjectID) return;
        
        //查找节点
        var trans = transform.FindDeep(highLightEventData.nodeName);
        if (!trans) return;
        
        //高亮
        var highlightObjectAction = new HighlightObjectAction()
        {
            highlightColor = highLightEventData.highlightColor,
            highlightWidth = highLightEventData.highlightWidth
        };
        
        SetHighlight(highlightObjectAction, data.isStartAction, trans);
    }

    private void OnGenerateWaveEvent(EventData param)
    {
        //提取事件数据
        var data = param.GetData<SceneController.ProgramEventData>();

        //提取自定义数据
        var highLightEventData = data?.actionData?.eventData as ProgramEvent.GenerateWaveEventParam;
        if (highLightEventData == null) return;
        
        //筛选ID
        if (EventObjectID != highLightEventData.EventObjectID) return;

        GenerateWave(data.isStartAction);        
    }
}