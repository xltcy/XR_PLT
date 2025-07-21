using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ClickableObject : MonoBehaviour
{
    /**
     * Introduction used to introduce by voice.
     */
    public string objIntroduction = "";
    /**
     * True: object will rotate in CLOSEUP state.
     */
    public bool doRotate = false;
    /**
     * Not applied.
     * Maybe enable/disable CLOSEUP state switch.
     */
    public bool closeUpDisplayEnable = true;

    /**
     * True: object will be display in particular visual angle in CLOSEUP mode.
     * Calculate by origin and target value which need to be set.
     */
    public bool closeUpMockDisplayTrans = false;

    /**
     * Maybe do nothing actually.
     */
    public Color normalColor;
    public Color highlightColor;
    public Color hideColor;

    /**
     * True: normalColor, highlightColor, hideColor will be set by Click3DObjectManager.
     */
    public bool colorFollowParent = true;
    /**
     * Current state.
     */
    public ClickableObjectState state = ClickableObjectState.NORMAL;

    // use to cal rotate angle when doRotate
    private float totalRotate = 0f;
    private const float ROTATE_DEGREE_ONCE = 1f;
    private Vector3 originPos;

    // use to cal trans and rotate when closeUpMockDisplayTrans is set.
    private Transform originTransform_CUMDT;
    private Vector3 oldForward_CUMDT;

    private List<Transform> rotateParts = new List<Transform>();


    // Start is called before the first frame update
    void Start()
    {
        originPos = transform.position;
        Transform[] childs = GetComponentsInChildren<Transform>();
        foreach (var item in childs)
        {
            if (item.gameObject.tag == "rotatable")
            {
                rotateParts.Add(item);
            }
        }
        if (doRotate && rotateParts.IsEmpty())
        {
            rotateParts.Add(transform);
        }

        foreach (var item in childs)
        {
            if (item.gameObject.name == "originPos_CUMDT")
            {
                originTransform_CUMDT = item;
                break;
            }
        }

        if (closeUpMockDisplayTrans)
        {
            oldForward_CUMDT = transform.forward;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (doRotate && state == ClickableObjectState.CLOSEUP)
        {
            foreach(var item in rotateParts)
            {
                item.Rotate(0, ROTATE_DEGREE_ONCE, 0);
            }
            totalRotate += ROTATE_DEGREE_ONCE;
            if (totalRotate >= 360)
            {
                totalRotate -= 360;
            }
        }
    }

    /**
     * Switch state.
     * NORMAL: default state. available to click.
     * INVISIBLE: when someone is in highlight, the other will be inactive.
     * HIGHLIGHT: do highlight action. available to long click. short click will turn back to NORMAL.
     * CLOSEUP: in highlight state, object will turn to this state by long click. move position and something else.
     */
    public void switchMode(ClickableObjectState newState)
    {
        // reset rotate
        if (doRotate && state == ClickableObjectState.CLOSEUP && newState != state)
        {
            foreach (var item in rotateParts)
            {
                item.Rotate(0, 360 - totalRotate, 0);
            }
            totalRotate = 0f;
        }
        if (state == ClickableObjectState.NORMAL)
        {
            originPos = transform.position;
            if (closeUpMockDisplayTrans)
            {
                oldForward_CUMDT = transform.forward;
            }
        }

        // ATTENTION state change here!
        state = newState;

        // setVisibility
        gameObject.SetActive(newState != ClickableObjectState.INVISIBLE);

        // set material color
        Color newColor;
        if (newState == ClickableObjectState.HIGHTLIGHT || newState == ClickableObjectState.CLOSEUP)
        {
            newColor = highlightColor;
        } else
        {
            newColor = normalColor;
        }
        SetRenderColor(newColor);

        // trans position
        if (newState != ClickableObjectState.CLOSEUP)
        {
            if (closeUpMockDisplayTrans)
            {
                transform.rotation = Quaternion.LookRotation(oldForward_CUMDT);
            }
            transform.position = originPos;
        } else
        {
            if (closeUpMockDisplayTrans)
            {
                Vector3 desRotate = FindObjectOfType<Camera>().transform.right;
                desRotate.Scale(new Vector3(-1f, -1f, -1f));
                transform.rotation = Quaternion.LookRotation(desRotate);
            }
            transform.Translate(GetTranslateMovement(), Space.World);
            
        }
    }

    private Vector3 GetTranslateMovement()
    {
        Camera arCamera = FindObjectOfType<Camera>();

        Vector3 trans = arCamera.transform.position - GetCenterPosInWorldSpace();
        
        if (closeUpMockDisplayTrans)
        {
            trans = arCamera.transform.position - originTransform_CUMDT.position;
            return trans;
        }
        
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        float r = boxCollider.size.magnitude / 2;
        Vector3 n = trans / trans.magnitude;
        return max(trans - n * r * 3, trans * 0.72f);
    }

    private Vector3 max(Vector3 v1, Vector3 v2)
    {
        if (v1.magnitude < v2.magnitude)
        {
            return v2;
        }
        return v1;
    }
    /**
     * return BoxCollider's center position in world space.
     */
    public Vector3 GetCenterPosInWorldSpace()
    {
        return transform.TransformPoint(gameObject.GetComponent<BoxCollider>().center);
    }

    /**
     * Object materials color change.
     */
    private void SetRenderColor(Color newColor)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            foreach (var item in renderer.materials)
            {
                item.color = newColor;
            }
        }

        // change children.
        //Renderer [] renderers =  GetComponentsInChildren<Renderer>();
        //foreach (var rendererChild in renderers)
        //{
        //    if (rendererChild != null)
        //    {
        //        foreach (var item in rendererChild.materials)
        //        {
        //            item.color = newColor;
        //        }
        //    }
        //}
    }

    public enum ClickableObjectState
    {
        // default mode
        NORMAL = 0,
        // one in highlight, other invisible; all invisible.
        INVISIBLE = 1,
        HIGHTLIGHT = 2,
        // long clicked in highlight will turn to closeup
        CLOSEUP = 3
    }
}
