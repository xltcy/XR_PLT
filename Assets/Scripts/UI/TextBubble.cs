using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Networking.UnityWebRequest;

public class TextBubble : MonoBehaviour
{
    [SerializeField] private int letterPerSecond = 1;
    public static TextBubble Instance;

    private GameObject bubble;

    private string tempText = "";

    private string nowText = "";

    private static int lineLength = 8;

    private static int lineCount = 4;

    public int lineNum = 0;    //显示几行

    private float speed = 0f;

    // If true, bubble will never display.Set false to use as normal.
    public bool muteAllTime = false;

    // use to prevent repeat same text request.
    private bool isShowingText = false;

    //private TextMeshProUGUI textGui;
    private Text textGui;
    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
        //textGui = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        textGui = transform.GetComponentInChildren<Text>();
    }

    public void SetText(string text)
    {
        if (muteAllTime)
        {
            return;
        }
        if (isShowingText && tempText == text)
        {
            return;
        }
        if (string.IsNullOrEmpty(text))
        {
            gameObject.SetActive(false);
            textGui.text = string.Empty;
            isShowingText = false;
            return;
        }

        gameObject.SetActive(true);
        //textGui.text = text;
        StartShowText(text);
    }

    public static void SetGlobalText(string text)
    {
        if (Instance != null)
        {
            Instance.SetText(text);
        }
    }

    public void StartShowText(string text)
    {
        //重复调用 文字打印机 方法.
        tempText = text;
        nowText = "";
        lineNum = 0;
        speed = 0f;
        InvokeRepeating("ShowText", 0, 0.1f);
    }


    private void ShowText()
    {
        isShowingText = true;
        if (speed < tempText.Length)
        {
            textGui.text = "";
            speed += 0.5f;

            nowText = tempText.Substring(0, (int)speed);
            if((int)speed == tempText.Length)
            {
                nowText = nowText.Substring(Math.Max(0, nowText.Length - 32), Math.Min(32, nowText.Length));
            }
            else
            {
                nowText = nowText.Substring((Math.Max(0, nowText.Length - 32) / 8) * 8, Math.Min(32, nowText.Length));
            }
            for(int i = 0; i < 4; ++i)
            {
                if(i * 8 < nowText.Length)
                {
                    textGui.text += nowText.Substring(i * 8, Math.Min(8, nowText.Length - i * 8));
                    if(i * 8 + 8 < nowText.Length)
                    {
                        textGui.text += "\n";
                    }
                }
            }
        }
        else
        {
            isShowingText = false;
            CancelInvoke();
        }
    }
}
