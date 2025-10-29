using UnityEngine;

public class UIUtils
{
    public static void SetVisible(Component component, bool visible)
    {
        if (component != null)
        {
            component.SetVisible(visible);
        }
    }
}