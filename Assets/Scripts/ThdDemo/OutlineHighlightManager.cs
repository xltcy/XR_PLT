using System.Collections;
using System.Collections.Generic;
using UniGLTF;
using UnityEngine;

public class OutlineHighlightManager : MonoBehaviour
{
    public void HighlightObject(GameObject target)
    {
        var otl = target.GetOrAddComponent<Outline>();
        otl.enabled = true;
        otl.OutlineColor = Color.yellow;
        otl.OutlineMode = Outline.Mode.OutlineVisible;
        otl.OutlineWidth = 3.0f;
    }

    public void HideHighlight(GameObject target)
    {
        var otl = target.GetOrAddComponent<Outline>();
        otl.enabled = false;
    }
}
