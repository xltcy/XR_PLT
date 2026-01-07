using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
//using TreeEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Demo : MonoBehaviour
{
    public GameObject thd;
    public GameObject up_roof;
    public GameObject down_roof;
    public GameObject monster;
    public GameObject door;
    public GameObject big_monster;

    private string _video_path = "Resources/video/thd_video-480p_24fps.mp4";
    private string _pose_path = "Resources/Pose/thd_video_pose.txt";

    private VideoPlayController videoPlayController;
    private CamTrajController camTrajController;
    private MaterialController materialController;

    void Start()
    {
        _video_path = Path.Combine(Application.dataPath, _video_path);
        _pose_path = Path.Combine(Application.dataPath, _pose_path);

        videoPlayController = ControllerRefer.VideoPlayController;
        camTrajController = ControllerRefer.CamTrajController;
        materialController = ControllerRefer.MaterialController;
    }

    void Update()
    {
        //if (Input.GetKeyUp(KeyCode.Alpha1)) ModelTreeNode.OneDofExplosion(???;
        //if (Input.GetKeyUp(KeyCode.Alpha2)) ModelTreeNode.TwoDofExplosion(??;
        //if (Input.GetKeyUp(KeyCode.Alpha3)) ModelTreeNode.ThreeDofExplosion(��??;
        //if (Input.GetKeyUp(KeyCode.Alpha4)) ModelTreeNode.ThreeDofExplosion(��??;
        //if (Input.GetKeyUp(KeyCode.Alpha5)) ModelTreeNode.TwoDofExplosion(����);
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            videoPlayController.LoadAndPlay(_video_path);
            camTrajController.StartCamMovement(_pose_path);
            materialController.SetTransparent(thd);
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            materialController.SetTransparent(thd);
        }
        else if (Input.GetKeyUp(KeyCode.Q))
        {
            materialController.RestoreMaterials();
        }
        else if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            monster.HighlightObject();
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            monster.HideHighlight();
        }
        else if (Input.GetKeyUp(KeyCode.Alpha4))
        {
            door.HighlightObject();
        }
        else if (Input.GetKeyUp(KeyCode.R))
        {
            door.HideHighlight();
        }
        else if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            up_roof.HighlightObject();
            down_roof.HighlightObject();
        }
        else if (Input.GetKeyUp(KeyCode.W))
        {
            up_roof.HideHighlight();
            down_roof.HideHighlight();
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            RotateController.RotateToTarget(big_monster, 45.0f, Camera.main.transform.up);
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            RotateController.RotateToTarget(big_monster, -45.0f, Camera.main.transform.up);
        }



    }
}
