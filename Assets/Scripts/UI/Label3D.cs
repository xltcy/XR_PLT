using TMPro;
using UnityEngine;

public class Label3D : MonoBehaviour
{
    public Camera arCamera;

    private GameObject label;

    void Start()
    {
        // 创建空容器
        GameObject container = new GameObject("SpeechBubble");
        label = container;
        container.transform.SetParent(transform, false);

        // 1. 背景图（Quad）
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "BubbleBG";
        bg.transform.SetParent(container.transform, false);
        bg.transform.localPosition = new Vector3(0, 0, 0.01f); // 稍后面一点
        bg.transform.localScale = new Vector3(4f, 2.5f, 1f); // 大小按贴图来调

        // 加载名为"3DLabel"的材质
        Material labelMat = Resources.Load<Material>("Materials/3DLabel");
        if (labelMat != null)
        {
            bg.GetComponent<Renderer>().material = labelMat;
        }
        else
        {
            Debug.LogWarning("找不到名为 '3DLabel' 的材质，请检查Resources文件夹里是否存在。");
        }

        // 2. TextMeshPro
        GameObject textObj = new GameObject("BubbleText");
        textObj.transform.SetParent(container.transform, false);

        var textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.text = "I am a label";
        textMesh.fontSize = 4;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.black;
        textObj.transform.localPosition = new Vector3(0, 0, 0);
        textObj.transform.localScale = Vector3.one;


        Vector3 offset = transform.right * 1.5f + transform.up * 1.5f;

        container.transform.position = transform.position + offset;
        //container.SetActive(false);
    }

    void Update()
    {
        if (transform == null || arCamera == null) return;
        label.transform.rotation = Quaternion.LookRotation(transform.position - arCamera.transform.position);
    }
}
