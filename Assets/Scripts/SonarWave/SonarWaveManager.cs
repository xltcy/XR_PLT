using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SonarWaveManager : MonoBehaviour
{
    public WaveGenerator sonar_subobj;

    private bool reflectorIsDirty = true;
    private Dictionary<string, WaveReflector> reflectorDic = new Dictionary<string, WaveReflector>();

    private void Start()
    {
        InitWaveGenerator();
    }

    private void InitWaveGenerator()
    {
        if (!sonar_subobj) this.transform.TryGetComponent<WaveGenerator>(out sonar_subobj);
        if (!sonar_subobj) sonar_subobj = this.transform.GetComponentInChildren<WaveGenerator>();
    }

    public void StartGenerate()
    {
        CheckAllReflector();
        
        sonar_subobj.StartGenerate();
    }
    public void StopGenerateAndDestroyWave()
    {
        // 停止发送声波
        sonar_subobj.StopGenerate();
        
        // 销毁发射波
        sonar_subobj.DestroyAllWave();
        
        //销毁所有反射波
        foreach (var reflector in reflectorDic)
        {
            reflector.Value.DestroyAllWave();
        }
    }
    
    // 名称匹配
    private void DestroyWaveRecursive(Transform current)
    {
        foreach (Transform child in current)
        {
            DestroyWaveRecursive(child);

            if (child.name.StartsWith("wave"))
            {
                Destroy(child.gameObject);
            }
        }
    }


    public void AddReflector(WaveReflector reflector)
    {
        var path = reflector.transform.GetFullPath();
        if (!reflectorDic.ContainsKey(path))
        {
            reflectorDic.Add(path, reflector);
        }
    }
    
    private void CheckAllReflector()
    {
        if (!reflectorIsDirty)
        {
            return;
        }
        
        var list = FindObjectsOfType<WaveReflector>();
        foreach (var item in list)
        {
            AddReflector(item);
        }

        reflectorIsDirty = false;
    }
}
