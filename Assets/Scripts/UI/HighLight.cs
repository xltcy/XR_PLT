using System.Collections.Generic;
using UnityEngine;

public class HighLight : MonoBehaviour
{
    public Color highlightcolor = Color.yellow; 
    [Header("声呐的三部分")]
    public Transform sonarAbove;
    public Transform boardMiddle;
    public Transform sonarUnderneath;

    private List<Material> sonarAboveMats = new List<Material>();
    private List<Material> boardMiddleMats = new List<Material>();
    private List<Material> sonarUnderneathMats = new List<Material>();

    private List<Color> sonarAboveOriginal = new List<Color>();
    private List<Color> boardMiddleOriginal = new List<Color>();
    private List<Color> sonarUnderneathOriginal = new List<Color>();

    void Start()
    {
        InitGroup(sonarAbove, sonarAboveMats, sonarAboveOriginal);
        InitGroup(boardMiddle, boardMiddleMats, boardMiddleOriginal);
        InitGroup(sonarUnderneath, sonarUnderneathMats, sonarUnderneathOriginal);
    }

    void InitGroup(Transform groupRoot, List<Material> matList, List<Color> colorList)
    {
        if (groupRoot == null) return;

        Renderer[] renderers = groupRoot.GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                matList.Add(mat);
                colorList.Add(mat.GetColor("_EmissionColor"));
            }
        }
    }

    /*void SetGroupEmission(List<Material> matList, Color color)
    {
        foreach (var mat in matList)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color);
        }
    }*/
    void SetGroupEmission(Transform show, Transform hide1,Transform hide2 , List<Material> matList, Color color)
    {
        show.gameObject.SetActive(true);
        hide1.gameObject.SetActive(false);
        hide2.gameObject.SetActive(false);
        foreach (var mat in matList)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color);
        }
    }
    /*void ResetGroupEmission(List<Material> matList, List<Color> original)
    {
        for (int i = 0; i < matList.Count; i++)
        {
            matList[i].SetColor("_EmissionColor", original[i]);
        }
    }*/
    void ResetGroupEmission(Transform show, Transform hide1, Transform hide2, List<Material> matList, List<Color> original)
    {
        show.gameObject.SetActive(true);
        hide1.gameObject.SetActive(true);
        hide2.gameObject.SetActive(true);
        for (int i = 0; i < matList.Count; i++)
        {
            matList[i].SetColor("_EmissionColor", original[i]);
        }
    }
    //以下是高亮函数（仅显示某部分）
    //public void HighlightAbove() => SetGroupEmission(sonarAboveMats, highlightcolor);
    public void HighlightAbove() => SetGroupEmission(sonarAbove, boardMiddle, sonarUnderneath, sonarAboveMats, highlightcolor);
    //public void HighlightMiddle() => SetGroupEmission(boardMiddleMats, highlightcolor);
    public void HighlightMiddle() => SetGroupEmission(boardMiddle, sonarAbove, sonarUnderneath, boardMiddleMats, highlightcolor);
    //public void HighlightUnderneath() => SetGroupEmission(sonarUnderneathMats, highlightcolor);
    public void HighlightUnderneath() => SetGroupEmission(sonarUnderneath, sonarAbove, boardMiddle, sonarUnderneathMats, highlightcolor);


    //以下是恢复原状态函数（三个部分全部显示）
    //public void ResetAbove() => ResetGroupEmission(sonarAboveMats, sonarAboveOriginal);
    public void ResetAbove() => ResetGroupEmission(sonarAbove, boardMiddle, sonarUnderneath, sonarAboveMats, sonarAboveOriginal);
    //public void ResetMiddle() => ResetGroupEmission(boardMiddleMats, boardMiddleOriginal);
    public void ResetMiddle() => ResetGroupEmission(boardMiddle, sonarAbove, sonarUnderneath, boardMiddleMats, boardMiddleOriginal);
    //public void ResetUnderneath() => ResetGroupEmission(sonarUnderneathMats, sonarUnderneathOriginal);
    public void ResetUnderneath() => ResetGroupEmission(sonarUnderneath, sonarAbove, boardMiddle, sonarUnderneathMats, sonarUnderneathOriginal);
}
