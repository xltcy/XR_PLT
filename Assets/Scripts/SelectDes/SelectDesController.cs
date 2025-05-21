using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectDesController : MonoBehaviour
{
    public GameObject itemPrefab;           // 拖入 ListItemPrefab
    public Transform contentParent;         // 拖入 ScrollView/Viewport/Content
    private List<SelectDesModel> desList;

    public Sprite[] images;
    public SelectDesActionInterface selectDesInterface;

    public Text hintText;
    public InputField testVoiceInputField;
    public bool useResources = false;

    private string resourceFolder = "SelectDesCoverImages";
    private string hintString = "请选择想要参观的区域\n点击屏幕上的选项或说“请带我参观某区域”";

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitState()
    {
        if (hintText != null)
        {
            hintText.text = hintString;
        }
        SpeechManager.SayFromStr(hintString);
        GenerateFakeData();
        GenerateSelectList();
    }

    public bool SelectDesByVoice(string inputstring)
    {
        foreach(var des in desList)
        {
            if (inputstring.Contains(des.title) || des.title.Contains(inputstring))
            {
                ListItemClickAction(des);
                return true;
            }
        }
        return false;
    }

    public void TestVoiceRecognize()
    {
        string input = testVoiceInputField.textComponent.text;
        Debug.Log($"test voice rec {input}");
        FindObjectOfType<VoiceController>().语音识别结果(input);
    }

    public List<SelectDesModel> GetSelectDesList()
    {
        return desList;
    }

    private void GenerateSelectList()
    {
        // 清空旧项目
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // 添加新项目
        foreach (var item in desList)
        {
            GameObject newItem = Instantiate(itemPrefab, contentParent);
            newItem.transform.Find("Text").GetComponent<Text>().text = item.title;
            newItem.transform.Find("Image").GetComponent<Image>().sprite = item.image;

            // 添加点击事件
            Button button = newItem.GetComponent<Button>();
            button.onClick.AddListener(() => item.onClickAction?.Invoke());
        }
    }

    private void GenerateFakeData()
    {
        List<string> titles = new List<string>();
        titles.Add("智慧海洋系统");
        titles.Add("虚实融合太空站");
        List<Sprite> imageList = LoadAllSpritesFromFolder();
        Debug.Log($"Load from Resource cnt: {imageList.Count}");
        if (!useResources)
        {
            imageList.Clear();
            foreach(var image in images)
            {
                imageList.Add(image);
            }
        }
        desList = new List<SelectDesModel>();
        for (int i = 0; i < imageList.Count; i++)
        {
            int index = i; // 捕获循环变量
            SelectDesModel item = new SelectDesModel
            {
                //title = $"选项 {i + 1}",
                title = titles[i % (titles.Count)] + i,
                image = imageList[i],
                
            };
            item.onClickAction = () =>
            {
                ListItemClickAction(item);
            };
            desList.Add(item);
        }
    }

    private List<Sprite> LoadAllSpritesFromFolder()
    {
        List<Sprite> sprites = new List<Sprite>();

        // Resources.LoadAll 会自动加载所有支持的资源类型
        Object[] loaded = Resources.LoadAll(resourceFolder, typeof(Texture2D));

        foreach (Object obj in loaded)
        {
            Texture2D tex = obj as Texture2D;
            if (tex == null) continue;

            // 创建 Sprite
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f) // 中心点
            );

            sprites.Add(sprite);
        }

        return sprites;
    }

    private void ListItemClickAction(SelectDesModel item)
    {
        Debug.Log($"Msg: 点击了选项 {item.title}");
        selectDesInterface?.OnSelectDesAt(item);
    }

    public interface SelectDesActionInterface
    {
        void OnSelectDesAt(SelectDesModel item);
    }
}
