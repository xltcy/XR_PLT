using System.Collections.Generic;
using UnityEngine;

public class HighLight : MonoBehaviour
{
    public Color highlightcolor = Color.yellow; 
    [Header("飞机的三部分")]
    public Transform p16Left;
    public Transform p16Cockpit;
    public Transform p16Right;


    void Start()
    {
        InitGroup(p16Left);
        InitGroup(p16Cockpit);
        InitGroup(p16Right);
    }

    void InitGroup(Transform groupRoot)
    {
        GameObject grobj = groupRoot.gameObject;
        if (groupRoot == null) return;

        grobj.AddComponent<Outline>();
        Outline otl = grobj.GetComponent<Outline>();
        otl.OutlineMode = Outline.Mode.OutlineAll;
        otl.OutlineColor = highlightcolor;
        otl.OutlineWidth = 10f;
        otl.enabled = false;
    }

    /*void SetGroupEmission(List<Material> matList, Color color)
    {
        foreach (var mat in matList)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color);
        }
    }*/
    void SetGroupEmission(Transform show)
    {
        show.gameObject.GetComponent<Outline>().enabled = true;
    }
    /*void ResetGroupEmission(List<Material> matList, List<Color> original)
    {
        for (int i = 0; i < matList.Count; i++)
        {
            matList[i].SetColor("_EmissionColor", original[i]);
        }
    }*/
    void ResetGroupEmission(Transform show)
    {
        show.gameObject.GetComponent<Outline>().enabled = false;
    }
    //以下是高亮函数（仅显示某部分）
    //public void HighlightAbove() => SetGroupEmission(sonarAboveMats, highlightcolor);
    public void HighlightLeft() => SetGroupEmission(p16Left);
    //public void HighlightMiddle() => SetGroupEmission(boardMiddleMats, highlightcolor);
    public void HighlightCockpit() => SetGroupEmission(p16Cockpit);
    //public void HighlightUnderneath() => SetGroupEmission(sonarUnderneathMats, highlightcolor);
    public void HighlightRight() => SetGroupEmission(p16Right);


    //以下是恢复原状态函数（三个部分全部显示）
    //public void ResetAbove() => ResetGroupEmission(sonarAboveMats, sonarAboveOriginal);
    public void ResetLeft() => ResetGroupEmission(p16Left);
    //public void ResetMiddle() => ResetGroupEmission(boardMiddleMats, boardMiddleOriginal);
    public void ResetCockpit() => ResetGroupEmission(p16Cockpit);
    //public void ResetUnderneath() => ResetGroupEmission(sonarUnderneathMats, sonarUnderneathOriginal);
    public void ResetRight() => ResetGroupEmission(p16Right);
}
