using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SonarWaveManager : BaseController
{
    public GameObject sonar_subobj;
    public Transform wall;
    public void StartGenerate()
    {
        sonar_subobj.GetComponent<WaveGenerator>().StartGenerate();
    }
    public void StopGenerateAndDestroyWave()
    {
        DestroyWaveRecursive(transform);
        DestroyWaveRecursive(wall);
        sonar_subobj.GetComponent<WaveGenerator>().StopGenerate();
    }
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
}
