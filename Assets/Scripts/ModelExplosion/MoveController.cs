using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoveController : MonoBehaviour
{
    private float speed=1.0f;
    private Vector3 target;
    private bool isMoving=false;

    public static void MoveToTarget(GameObject model, Vector3 target)
    {
        var moveController = model.GetComponent<MoveController>();
        if (moveController == null)
        {
            moveController = model.AddComponent<MoveController>();
        }
        moveController.target = target;
        moveController.isMoving = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {
            // 计算每帧需要移动的距离
            float step = speed * Time.deltaTime;
            // 移动模型
            transform.position = Vector3.MoveTowards(transform.position, target, step);

            // 检查是否到达目标位置
            if (Vector3.Distance(transform.position, target)<=1e-2)
            {
                isMoving = false;
            }
        }
    }
}
