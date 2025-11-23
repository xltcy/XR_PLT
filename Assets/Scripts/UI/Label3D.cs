using TMPro;
using UnityEngine;

public class Label3D : MonoBehaviour
{
    public Camera arCamera;
    public GameObject labelTarget;

    private GameObject label;

    void Start()
    {
        label = null; 
    }

    void Update()
    {
        if (transform == null || arCamera == null) return;
        if(label != null)
            label.transform.rotation = Quaternion.LookRotation(label.transform.position - arCamera.transform.position);
    }

    public void ShowLable()
    {
        // 创建空容器
        GameObject container = new GameObject("SpeechBubble");
        label = container;
        container.transform.SetParent(labelTarget.transform, false);

        // 1. 背景图（Quad）
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "BubbleBG";
        bg.transform.SetParent(container.transform, false);
        bg.transform.localPosition = new Vector3(0, 0, 0.01f); // 稍后面一点
        bg.transform.localScale = new Vector3(1.2f, 0.4f, 0.1f); // 大小按贴图来调

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
        textMesh.text = "C750D双屏图像声呐";
        textMesh.fontSize = 1;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.black;
        textObj.transform.localPosition = new Vector3(0, 0, 0);
        textObj.transform.localScale = Vector3.one;

        //TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts/simhei SDF");
        //if (fontAsset != null)
        //{
        //    textMesh.font = fontAsset;
        //}
        //else
        //{
        //    Debug.LogWarning("未能加载 simhei SDF 字体，请检查 Resources 路径是否正确。");
        //}


        Vector3 offset = new Vector3(0, 0.3f, -0.4f);

        //container.transform.position = labelTarget.transform.position + offset;
        container.transform.localPosition = offset;
        //container.SetActive(false);
        //container.transform.rotation = Quaternion.LookRotation(container.transform.position - arCamera.transform.position);
    }

    public void HideLabel()
    {
        foreach (Transform child in labelTarget.transform)
        {
            if (child.name.StartsWith("SpeechBubble"))
            {
                Destroy(child.gameObject);
            }
        }
    }
}
