using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectDesController : MonoBehaviour
{
    public GameObject itemPrefab;           // ���� ListItemPrefab
    public Transform contentParent;         // ���� ScrollView/Viewport/Content
    private List<SelectDesModel> desList;

    public Sprite[] images;
    public SelectDesActionInterface selectDesInterface;

    public Text hintText;
    public InputField testVoiceInputField;
    public bool useResources = false;

    private string resourceFolder = "SelectDesCoverImages";
    private string hintString = "��ѡ����Ҫ�ι۵�����\n�����Ļ�ϵ�ѡ���˵������Ҳι�ĳ����";

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
        FindObjectOfType<VoiceController>().����ʶ����(input);
    }

    public List<SelectDesModel> GetSelectDesList()
    {
        return desList;
    }

    private void GenerateSelectList()
    {
        // ��վ���Ŀ
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // �������Ŀ
        foreach (var item in desList)
        {
            GameObject newItem = Instantiate(itemPrefab, contentParent);
            newItem.transform.Find("Text").GetComponent<Text>().text = item.title;
            newItem.transform.Find("Image").GetComponent<Image>().sprite = item.image;

            // ��ӵ���¼�
            Button button = newItem.GetComponent<Button>();
            button.onClick.AddListener(() => item.onClickAction?.Invoke());
        }
    }

    private void GenerateFakeData()
    {
        List<string> titles = new List<string>();
        titles.Add("�ǻۺ���ϵͳ");
        titles.Add("��ʵ�ں�̫��վ");
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
            int index = i; // ����ѭ������
            SelectDesModel item = new SelectDesModel
            {
                //title = $"ѡ�� {i + 1}",
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

        // Resources.LoadAll ���Զ���������֧�ֵ���Դ����
        Object[] loaded = Resources.LoadAll(resourceFolder, typeof(Texture2D));

        foreach (Object obj in loaded)
        {
            Texture2D tex = obj as Texture2D;
            if (tex == null) continue;

            // ���� Sprite
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f) // ���ĵ�
            );

            sprites.Add(sprite);
        }

        return sprites;
    }

    private void ListItemClickAction(SelectDesModel item)
    {
        Debug.Log($"Msg: �����ѡ�� {item.title}");
        selectDesInterface?.OnSelectDesAt(item);
    }

    public interface SelectDesActionInterface
    {
        void OnSelectDesAt(SelectDesModel item);
    }
}
