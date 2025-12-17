using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Manage to play video on an ARTrackedImage.
/// </summary>
public class VideoManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    public VideoClip[] clips;

    public ARTrackedImage trackedImage;

    private string video_url;


    // Start is called before the first frame update
    void Start()
    {
        if (videoPlayer)
        {
            videoPlayer.loopPointReached += OnVideoFinish;
            videoPlayer.prepareCompleted += OnVideoPrepared;
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer)
        {
            videoPlayer.loopPointReached -= OnVideoFinish;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (trackedImage != null)
        {
            //trackedImage原点：识别图的几何中心
            //trackedImage.transform.right → 图片的水平方向 图像的宽度方向
            //trackedImage.transform.up → 图片的竖直方向 图像的高度方向
            //trackedImage.transform.forward → 图片的法线（垂直于图片） 法线方向（垂直于图片，指向相机这一侧）
            // Keep tracking ARTrackedImage's transform.
            transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.y);
            Debug.Log("scale" + trackedImage.size);
        }
    }

    public void PlayShengnaVideo()
    {
        //ControllerRefer.MeshController.SummonScreen();
        PlayVideo("test");
    }

    public void TestPlayVideo()
    {
        PlayVideo("test");
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        vp.Play();
    }

    public void PlayVideo(string name)
    {
        gameObject.SetActive(true);
        //todo
        // string name = "video";
        
        
        //优先寻找clips里面的视频
        Regex regex = new Regex(name);
        if (clips != null)
        {
            foreach (var clip in clips)
            {
                if (clip != null && regex.IsMatch(clip.originalPath))
                {
                    videoPlayer.clip = clip;
                    break;
                }
            }
        }

        var needLoad = videoPlayer.clip == null;
        if (!needLoad)
        {
            var list = name.Split('/');
            needLoad = list[list.Length - 1] != videoPlayer.clip.name;
        }
        
        if (needLoad)
        {
            //其次寻找本地视频文件
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = Resources.Load<VideoClip>(name);
        }


        if (videoPlayer.clip != null)
        {
            videoPlayer.Prepare();
        }
        
        Debug.Log("videoUrl: " + video_url);
    }

    void OnVideoPrepare(VideoPlayer vp)
    {
        int videoWidth = (int)vp.texture.width;
        int videoHeight = (int)vp.texture.height;

        videoPlayer.transform.localScale = new Vector3(
            1, (float)videoHeight / videoWidth, videoPlayer.transform.localScale.z
            );
    }

    void OnVideoFinish(VideoPlayer vp)
    {
        Debug.Log("视频播放结束");

        gameObject.SetActive(false);
        // todo 
    }

    public void SetScreen()
    {
         
    }

    public void PlayVideo()
    {
        videoPlayer.Play();
    }

    public void PauseVideo()
    {
        videoPlayer.Pause();
    }

    public void StopVideo()
    {
        videoPlayer.Stop();
    }

    public void ForceStop()
    {
        videoPlayer.Stop();
    }
}
