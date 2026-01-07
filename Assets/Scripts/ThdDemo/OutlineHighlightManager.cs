using System.Collections;
using System.Collections.Generic;
using UniGLTF;
using UnityEngine;

public static class GameObjectOutlineExtensions
{
    public static void HighlightObject(this GameObject target)
    {
        var otl = target.GetOrAddComponent<Outline>();
        otl.enabled = true;
        otl.OutlineColor = Color.yellow;
        otl.OutlineMode = Outline.Mode.OutlineVisible;
        otl.OutlineWidth = 3.0f;
    }

    public static void HideHighlight(this GameObject target)
    {
        var otl = target.GetOrAddComponent<Outline>();
        otl.enabled = false;
    }
}
