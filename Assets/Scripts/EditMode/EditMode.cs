using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EditMode : BaseController
{
    // Start is called before the first frame update
    public Slider 屏幕大小调节;

    public Slider 模型大小调节;

    public GameObject screen;

    private Vector3 screenScale;

    public Dropdown 移动0旋转1;

    public Dropdown 操作幅度;

    public Dropdown 操作对象;

    private float trans_amp = 0.01f;
    
    private float rot_amp = 0.1f;

    private int ratio = 1;

    public TMP_InputField cx_Input;

    public TMP_InputField cy_Input;

    public TMP_InputField focal_Input;

    public Slider cy滑动条;

    public Text cx_cy_focal;

    private float cx;

    private float cy;

    private float focal;

    private float cx_cy = 1.6f;

    public Camera arCamera;

    public GameObject UI_调试_Mesh;
    public GameObject UI_调试_屏幕;
    public GameObject UI_调试_相机;

    public Toggle Toggle_Mesh;
    public Toggle Toggle_屏幕;
    public Toggle Toggle_相机;

    void Start()
    {
        SetSliderValueChangeListener(屏幕大小调节, 屏幕Resize);
        SetSliderValueChangeListener(模型大小调节, 模型Resize);
        SetDropDownValueChangeListener(操作幅度, 幅度切换);
        SetInputFieldValueChangeListener(cx_Input, cxChange);
        SetInputFieldValueChangeListener(cy_Input, cyChange);
        SetInputFieldValueChangeListener(focal_Input, focalChange);
        SetSliderValueChangeListener(cy滑动条, cySlide);

        if (screen != null)
        {
            screenScale = screen.transform.localScale;
        } else
        {
            screenScale = new Vector3(1, 1, 1);
        }

        Toggle_Mesh.onValueChanged.AddListener(value => {
            UI_调试_Mesh.SetActive(value);
        });
        Toggle_屏幕.onValueChanged.AddListener(value => {
            UI_调试_屏幕.SetActive(value);
        });
        Toggle_相机.onValueChanged.AddListener(value => {
            UI_调试_相机.SetActive(value);
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (cx_cy_focal != null)
        {
            cx_cy_focal.text = "cx = " + arCamera.GetComponent<Camera>().sensorSize.x + "\n" + "cy = " + arCamera.GetComponent<Camera>().sensorSize.y + "\n" + "focal = " + arCamera.GetComponent<Camera>().focalLength;
        }
    }

    void 屏幕Resize(float value)
    {
        screen.transform.localScale = screenScale * value;
    }

    void 模型Resize(float value)
    {
        GameObject meshObj = GetMeshObj();
        if(meshObj != null)
        {
            meshObj.transform.localScale = Vector3.one * value;
        }
    }

    private float StrToFloat(object FloatString)
    {
        float result;
        if (FloatString != null)
        {
            if (float.TryParse(FloatString.ToString(), out result))
                return result;
            else
            {
                return (float)0.00;
            }
        }
        else
        {
            return (float)0.00;
        }
    }

    void cxChange(string value)
    {
        cx = StrToFloat(value);
        cy = cx / cx_cy;
        arCamera.GetComponent<Camera>().sensorSize = new Vector2(cx, cy);
    }

    void cyChange(string value)
    {
        cy = StrToFloat(value);
        cx = cy * cx_cy;
        arCamera.GetComponent<Camera>().sensorSize = new Vector2(cx, cy);
    }

    void focalChange(string value)
    {
        focal = StrToFloat(value);
        arCamera.GetComponent<Camera>().focalLength = focal;
    }

    void cySlide(float value)
    {
        cy = value;
        cx = cy * cx_cy;
        arCamera.GetComponent<Camera>().sensorSize = new Vector2(cx, cy);
    }

    void 幅度切换(int v)
    {
        switch (v)
        {
            case 0:ratio = 1; break;
            case 1:ratio = 2; break;
            case 2:ratio = 5; break;
            case 3:ratio = 10;break;
            default:ratio = 1; break;
        }
    }

    void SetSliderValueChangeListener(Slider slider, UnityAction<float> listener)
    {
        if (slider != null)
        {
            slider.onValueChanged.AddListener((value) => {
                listener(value);
            });
        }
    }

    void SetDropDownValueChangeListener(Dropdown dropdown, UnityAction<int> listener)
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener((value) => {
                listener(value);
            });
        }
    }

    void SetInputFieldValueChangeListener(TMP_InputField inputField, UnityAction<string> listener)
    {
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener((value) => {
                listener(value);
            });
        }
    }

    public void Left()
    {
        GameObject meshObj = GetMeshObj();
        if (meshObj != null)
        {
            if(移动0旋转1.value == 0)
            {
                meshObj.transform.Translate(new Vector3(-trans_amp * ratio, 0, 0));
            }
            else
            {
                meshObj.transform.Rotate(new Vector3(-rot_amp * ratio, 0, 0));
            }
        }
    }

    public void Right()
    {
        GameObject meshObj = GetMeshObj();
        if (meshObj != null)
        {
            if (移动0旋转1.value == 0)
            {
                meshObj.transform.Translate(new Vector3(trans_amp * ratio, 0, 0));
            }
            else
            {
                meshObj.transform.Rotate(new Vector3(rot_amp * ratio, 0, 0));
            }
        }
    }
    

    public void Up()
    {
        GameObject meshObj = GetMeshObj();
        if (meshObj != null)
        {
            if (移动0旋转1.value == 0)
            {
                meshObj.transform.Translate(new Vector3(0, trans_amp * ratio, 0));
            }
            else
            {
                meshObj.transform.Rotate(new Vector3(0, rot_amp * ratio, 0));
            }
        }
    }

    public void Down()
    {
        GameObject meshObj = GetMeshObj();
        if (meshObj != null)
        {
            if (移动0旋转1.value == 0)
            {
                meshObj.transform.Translate(new Vector3(0, -trans_amp * ratio, 0));
            }
            else
            {
                meshObj.transform.Rotate(new Vector3(0, -rot_amp * ratio, 0));
            }
        }
    }

    public void Forward()
    {
        GameObject meshObj = GetMeshObj();
        if (meshObj != null)
        {
            if (移动0旋转1.value == 0)
            {
                meshObj.transform.Translate(new Vector3(0, 0, trans_amp * ratio));
            }
            else
            {
                meshObj.transform.Rotate(new Vector3(0, 0, rot_amp * ratio));
            }
        }
    }

    public void Back()
    {
        GameObject meshObj = GetMeshObj();
        if (meshObj != null)
        {
            if (移动0旋转1.value == 0)
            {
                meshObj.transform.Translate(new Vector3(0, 0, -trans_amp * ratio));
            }
            else
            {
                meshObj.transform.Rotate(new Vector3(0, 0, -rot_amp * ratio));
            }
        }
    }

    private GameObject GetMeshObj()
    {
        string tag = "Mesh";
        if (操作对象.value == 1)
        {
            tag = "ground";
        }
        GameObject meshObj = GameObject.FindGameObjectWithTag(tag);
        return meshObj;
    }
}
