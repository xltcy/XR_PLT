using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RotateController : MonoBehaviour
{
    private float speed = 120.0f;
    private float targetAngle;
    private float currentAngle;
    private float rotateX;
    private float rotateY;
    private bool isRotating = false;

    public static void RotateToTarget(GameObject model, float angle)
    {
        RotateController rotator = model.GetComponent<RotateController>();
        if (rotator == null)
        {
            rotator = model.AddComponent<RotateController>();
        }

        rotator.rotateX = model .transform.rotation.eulerAngles.x;
        rotator.rotateY = model.transform.rotation.eulerAngles.y;
        rotator.currentAngle = model.transform.rotation.eulerAngles.z;
        rotator.targetAngle = rotator.currentAngle + angle;
        rotator.isRotating = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isRotating) { 
            float step = speed * Time.deltaTime;
            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, step);
            transform.rotation = Quaternion.Euler(rotateX, rotateY, currentAngle);

            if (Mathf.Abs(currentAngle - targetAngle) <= 1e-3)
            {
                isRotating = false;
                var plane = gameObject.transform.Find("Plane");
                if (plane != null)
                {
                    plane.GetComponent<ModelTreeNode>().InitPlane(transform.rotation, plane.transform.localScale.x);
                }
            }
        }
    }

    public static void TestSwipeLeft(GameObject model)
    {
        RotateToTarget(model, 180.0f);
    }
    public static void TestSwipeRight(GameObject model)
    {
        RotateToTarget(model, -180.0f);

    }
}
