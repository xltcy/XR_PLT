using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using TreeEditor;
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

    private VideoPlayManager _video_play_manager;
    private CamTrajManager _cam_traj_manager;
    private OutlineHighlightManager _outline_highlight_manager;
    private MaterialManager _material_manager;

    void Start()
    {
        _video_path = Path.Combine(Application.dataPath, _video_path);
        _pose_path = Path.Combine(Application.dataPath, _pose_path);

        _video_play_manager = FindObjectOfType<VideoPlayManager>();
        _cam_traj_manager = FindObjectOfType<CamTrajManager>();
        _outline_highlight_manager = FindObjectOfType<OutlineHighlightManager>();
        _material_manager = FindObjectOfType<MaterialManager>();
    }

    void Update()
    {
        //if (Input.GetKeyUp(KeyCode.Alpha1)) ModelTreeNode.OneDofExplosion(???;
        //if (Input.GetKeyUp(KeyCode.Alpha2)) ModelTreeNode.TwoDofExplosion(??;
        //if (Input.GetKeyUp(KeyCode.Alpha3)) ModelTreeNode.ThreeDofExplosion(ùù??;
        //if (Input.GetKeyUp(KeyCode.Alpha4)) ModelTreeNode.ThreeDofExplosion(ùù??;
        //if (Input.GetKeyUp(KeyCode.Alpha5)) ModelTreeNode.TwoDofExplosion(ùùùù);
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            _video_play_manager.LoadAndPlay(_video_path);
            _cam_traj_manager.StartCamMovment(_pose_path);
            _material_manager.SetTransparent(thd);
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            _material_manager.SetTransparent(thd);
        }
        else if (Input.GetKeyUp(KeyCode.Q))
        {
            _material_manager.RestoreMaterials();
        }
        else if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            _outline_highlight_manager.HighlightObject(monster);
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            _outline_highlight_manager.HideHighlight(monster);
        }
        else if (Input.GetKeyUp(KeyCode.Alpha4))
        {
            _outline_highlight_manager.HighlightObject(door);
        }
        else if (Input.GetKeyUp(KeyCode.R))
        {
            _outline_highlight_manager.HideHighlight(door);
        }
        else if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            _outline_highlight_manager.HighlightObject(up_roof);
            _outline_highlight_manager.HighlightObject(down_roof);
        }
        else if (Input.GetKeyUp(KeyCode.W))
        {
            _outline_highlight_manager.HideHighlight(up_roof);
            _outline_highlight_manager.HideHighlight(down_roof);
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
