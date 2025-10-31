using System.Collections.Generic;
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

    private static readonly Dictionary<string, string> TypeNameDic = new Dictionary<string, string>()
    {
        {"UnityEngine.UI.Button", "按钮"},
        {"UnityEngine.UI.Transform", "形变"},
        {"UnityEngine.UI.Text", "文本"},
        {"UnityEngine.UI.Image", "图片"},
        {"UnityEngine.UI.Toggle", "开关"},
        {"UnityEngine.UI.Slider", "滑动条"},
        {"UnityEngine.UI.InputField", "输入框"},
        {"UnityEngine.UI.Dropdown", "下拉框"},
        {"UnityEngine.UI.ScrollRect", "滚动视图"},
        {"UnityEngine.UI.RawImage", "原始图片"},
        {"UnityEngine.UI.ContentSizeFitter", "内容大小适配器"},
        {"UnityEngine.UI.HorizontalLayoutGroup", "水平布局组"},
        {"UnityEngine.UI.VerticalLayoutGroup", "垂直布局组"},
        {"UnityEngine.UI.GridLayoutGroup", "网格布局组"},
        {"UnityEngine.UI.LayoutElement", "布局元素"},
    };
    public static string GetComponentTypeName(string typeName)
    {
        if (TypeNameDic.TryGetValue(typeName, out string result))
        {
            return result;
        }
        return "NULL";
    }
}