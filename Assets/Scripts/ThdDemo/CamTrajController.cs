using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CamTrajController : BaseController
{
    public Camera _cam;
    private Queue<string> _pose_lines = new Queue<string>();

    public void StartCamMovement(string pose_path)
    {
        ReadAllLines(pose_path);
        InvokeRepeating(nameof(PlaceNextCam), 0.0f, 0.2f);
    }

    private void ReadAllLines(string pose_path)
    {
        var lines = File.ReadAllLines(pose_path);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
                _pose_lines.Enqueue(line.Trim());
        }
    }

    private void PlaceNextCam()
    {
        if (_pose_lines.Count > 0)
        {
            var line = _pose_lines.Dequeue();
            var c2w = MatrixUtil.ParseMatrix(line);
            string s_m = "1 0 0 0 0 -1 0 0 0 0 1 0 0 0 0 1";
            var m = MatrixUtil.ParseMatrix(s_m);
            c2w = m * c2w * m.transpose;

            var pose = MatrixUtil.GetPose(c2w);
            _cam.transform.position = pose.position;
            _cam.transform.rotation = pose.rotation;
        }
        else
        {
            CancelInvoke();
        }
    }
}
