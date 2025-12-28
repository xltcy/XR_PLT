using System.Collections;
using System.Collections.Generic;
using UniGLTF;
using UnityEditor;
using UnityEngine;

public class RotateController : BaseController
{
    private float speed = 120.0f;
    private float targetAngle;
    private float currentAngle;
    private bool isRotating = false;
    private int clockwise;
    private Vector3 rotateAxis;

    public static void RotateToTarget(GameObject model, float angle)
    {
        RotateController rotator = model.GetOrAddComponent<RotateController>();

        if (angle > 0)
        {
            rotator.clockwise = 1;
        }
        else
        {
            rotator.clockwise = -1;
        }

        rotator.currentAngle = 0;
        rotator.targetAngle = angle;
        rotator.isRotating = true;
        rotator.rotateAxis = model.transform.up;
    }
    public static void RotateToTarget(GameObject model, float angle, Vector3 rotateAxis)
    {
        RotateController rotator = model.GetOrAddComponent<RotateController>();

        if (angle > 0)
        {
            rotator.clockwise = 1;
        }
        else
        {
            rotator.clockwise = -1;
        }

        rotator.currentAngle = 0;
        rotator.targetAngle = angle;
        rotator.isRotating = true;
        rotator.rotateAxis = rotateAxis;
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isRotating) {
            var rotate_center = transform.GetChildByName("center").position;
            transform.RotateAround(rotate_center, rotateAxis, clockwise * speed * Time.deltaTime);

            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, speed * Time.deltaTime);
            if (Mathf.Abs(currentAngle - targetAngle) <= 1e-3)
            {
                isRotating = false;
            }
        }
    }
}
