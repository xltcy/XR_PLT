using System.Collections.Generic;
using UnityEngine;

public class HighLight : MonoBehaviour
{
    public Color highlightcolor = Color.yellow; 
    [Header("声呐的三部分")]
    public Transform sonarAbove;
    public Transform boardMiddle;
    public Transform sonarUnderneath;


    void Start()
    {
        InitGroup(sonarAbove);
        InitGroup(boardMiddle);
        InitGroup(sonarUnderneath);
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
    void SetGroupEmission(Transform show, Transform hide1,Transform hide2)
    {
        //show.gameObject.SetActive(true);
        //hide1.gameObject.SetActive(false);
        //hide2.gameObject.SetActive(false);
        hide1.gameObject.GetComponent<Outline>().enabled = false;
        hide2.gameObject.GetComponent<Outline>().enabled = false;
        show.gameObject.GetComponent<Outline>().enabled = true;
    }
    /*void ResetGroupEmission(List<Material> matList, List<Color> original)
    {
        for (int i = 0; i < matList.Count; i++)
        {
            matList[i].SetColor("_EmissionColor", original[i]);
        }
    }*/
    void ResetGroupEmission(Transform show, Transform hide1, Transform hide2)
    {
        show.gameObject.SetActive(true);
        hide1.gameObject.SetActive(true);
        hide2.gameObject.SetActive(true);
        show.gameObject.GetComponent<Outline>().enabled = false;
    }
    //以下是高亮函数（仅显示某部分）
    //public void HighlightAbove() => SetGroupEmission(sonarAboveMats, highlightcolor);
    public void HighlightAbove() => SetGroupEmission(sonarAbove, boardMiddle, sonarUnderneath);
    //public void HighlightMiddle() => SetGroupEmission(boardMiddleMats, highlightcolor);
    public void HighlightMiddle() => SetGroupEmission(boardMiddle, sonarAbove, sonarUnderneath);
    //public void HighlightUnderneath() => SetGroupEmission(sonarUnderneathMats, highlightcolor);
    public void HighlightUnderneath() => SetGroupEmission(sonarUnderneath, sonarAbove, boardMiddle);


    //以下是恢复原状态函数（三个部分全部显示）
    //public void ResetAbove() => ResetGroupEmission(sonarAboveMats, sonarAboveOriginal);
    public void ResetAbove() => ResetGroupEmission(sonarAbove, boardMiddle, sonarUnderneath);
    //public void ResetMiddle() => ResetGroupEmission(boardMiddleMats, boardMiddleOriginal);
    public void ResetMiddle() => ResetGroupEmission(boardMiddle, sonarAbove, sonarUnderneath);
    //public void ResetUnderneath() => ResetGroupEmission(sonarUnderneathMats, sonarUnderneathOriginal);
    public void ResetUnderneath() => ResetGroupEmission(sonarUnderneath, sonarAbove, boardMiddle);
}
