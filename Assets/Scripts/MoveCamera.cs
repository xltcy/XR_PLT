using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    private float moveSpeed = 5f; // 相机移动速度
    private float lookSpeed = 2f;  // 相机旋转速度

    private float pitch = 0f; // 上下旋转角度
    private float yaw = 0f;   // 左右旋转角度

    public Camera camera;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 键盘控制相机移动
        Vector3 move = Vector3.zero;
        Debug.Log("Msg in CameraMoving: " + move.ToString());

        // WSAD 控制前后左右
        if (Input.GetKey(KeyCode.W)) move += camera.transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= camera.transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= camera.transform.right;
        if (Input.GetKey(KeyCode.D)) move += camera.transform.right;

        // QE 控制上下
        if (Input.GetKey(KeyCode.Q)) move -= transform.up;
        if (Input.GetKey(KeyCode.E)) move += transform.up;

        camera.transform.position += move * moveSpeed * Time.deltaTime;

        // 鼠标控制视角移动仅在按下左键时生效
        if (Input.GetMouseButton(0)) // 0表示鼠标左键
        {
            yaw += Input.GetAxis("Mouse X") * lookSpeed;
            pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
            camera.transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
        }
    }
}
