using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Manage to play video on an ARTrackedImage.
/// </summary>
public class VideoManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    public VideoClip[] clips; 

    private string video_url;


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void PlayShengnaVideo()
    {
        //FindObjectOfType<MeshController>().SummonScreen();
        PlayVideo("shengna");
    }

    public void TestPlayVideo()
    {
        PlayVideo("shengna");
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
        Debug.Log(" ”∆µ≤•∑≈Ω· ¯");

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
