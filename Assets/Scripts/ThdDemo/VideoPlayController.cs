using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPlayController : BaseController
{
    public RawImage videoDisplay;
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;

    public override void OnRegister()
    {
        base.OnRegister();
        
        
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        audioSource = gameObject.AddComponent<AudioSource>();

        // ����VideoPlayer
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;

        // ������Ƶ���
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);

        // ������Ⱦ��RawImage
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 24);
        videoPlayer.targetTexture = renderTexture;
        videoDisplay.texture = renderTexture;
    }

    public void LoadAndPlay(string videoPath)
    {
        videoPlayer.url = videoPath;
        videoPlayer.Play();
    }
}
