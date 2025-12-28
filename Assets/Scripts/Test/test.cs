using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class test : MonoBehaviour
{
    public Camera _cam;

    // Start is called before the first frame update
    void Start()
    {
        string ss = "1.0 7.596296e-05 8.480836e-05 8.809019e-06 -7.5965116e-05 1.0 2.528223e-05 -6.398597e-05 -8.480644e-05 -2.5288671e-05 1.0 -6.988847e-05 0.0 0.0 0.0 1.0";
        Matrix4x4 mm = MatrixUtil.ParseMatrix(ss);
        var cam_pos = _cam.transform.position;
        var cam_rot = _cam.transform.rotation;
        var cam_scale = _cam.transform.localScale;
        _cam.transform.position = cam_pos;
        _cam.transform.rotation = cam_rot;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
