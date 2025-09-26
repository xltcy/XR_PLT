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
            transform.position = trackedImage.transform.position;
            transform.rotation = trackedImage.transform.rotation;
            transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.y);
            Debug.Log("VideoManager" + trackedImage.transform.position + "rotation" + trackedImage.transform.rotation + "scale" + trackedImage.size);
        }
    }

    public void PlayShengnaVideo()
    {
        //FindObjectOfType<MeshController>().SummonScreen();
        PlayVideo("test");
    }

    public void TestPlayVideo()
    {
        PlayVideo("test");
    }

    public void PlayVideo(string name)
    {
        gameObject.SetActive(true);
        //todo
        // string name = "video";
        Regex regex = new Regex(name);
        foreach (var clip in clips)
        {
            if (regex.IsMatch(clip.originalPath))
            {
                //videoPlayer.prepareCompleted += OnVideoPrepare;
                videoPlayer.loopPointReached += OnVideoFinish;
                videoPlayer.clip = clip;
                videoPlayer.Play();
                break;
            }
        }

        // video_url = Application.persistentDataPath + "/Videos/video_file.mp4";
        // video_url = "T:/Desktop/video.mp4";
        Debug.Log("videoUrl: " + video_url);
        // videoPlayer.url = video_url;
        // videoPlayer.Play();
        // videoPlayer.Prepare();

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
