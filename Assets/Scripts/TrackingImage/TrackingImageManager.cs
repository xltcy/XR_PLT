using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TrackingImageManager : MonoBehaviour
{
    public ARTrackedImageManager trackedImageManager;

    public List<ARTrackedImage> trackedImages = new List<ARTrackedImage>();
    // trackedImageManager's referenceLibrary
    private MutableRuntimeReferenceImageLibrary runtimeLibrary;

    private List<ActionTriggerData> triggers = new List<ActionTriggerData>();

    // Start is called before the first frame update
    void Start()
    {
        DisableTrackImage();
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void InitTriggeredImage(List<ActionTriggerData> triggerList)
    {
        triggers.AddRange(triggerList);
        if (triggers.IsEmpty())
        {
            return;
        }
        EnableTrackImage();
        foreach(var trigger in triggers)
        {
            // 从 Resources 加载
            Texture2D tex = Resources.Load<Texture2D>(trigger.imagePath); // 不要写扩展名
            if (tex != null)
            {
                AddImage(tex, trigger.imageName, 0.1f); // 0.1f = 实际物理尺寸 (米)
            }
            else
            {
                Debug.LogError("Image not found in Resources!");
            }
        }
    }

    void AddImage(Texture2D texture, string name, float widthInMeters)
    {
        if (runtimeLibrary == null)
        {
            Debug.LogError("Runtime library not initialized.");
            return;
        }

        var job = runtimeLibrary.ScheduleAddImageWithValidationJob(texture, name, widthInMeters);
        StartCoroutine(CheckJob(job, name));
    }

    private IEnumerator CheckJob(AddReferenceImageJobState job, string name)
    {
        while (!job.jobHandle.IsCompleted)
            yield return null;

        job.jobHandle.Complete();

        if (job.status == AddReferenceImageJobStatus.Success)
            Debug.Log($"? Added image {name} successfully!");
        else
            Debug.LogError($"? Failed to add image {name}: {job.status}");
    }

    /// <summary>
    /// 把模型放置到被追踪图片的位置
    /// </summary>
    /// <param name="model">需要设置位置的模型</param>
    /// <param name="trackedImage">被Tracked Image Manager追踪到的图片</param>
    /// <returns></returns>
    IEnumerator AlignModelToTrackedImage(List<ActionTriggerData> triggers, ARTrackedImage trackedImage)
    {
        yield return new WaitUntil(() =>
        {
            return trackedImage.transform.position.sqrMagnitude > float.Epsilon;
        });
        triggers.ForEach(trigger => FindObjectOfType<SceneController>().ConsoleImageRecognizeTrigger(trigger, trackedImage));
    }

    // use to console when a image is tracked.
    public void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        LogInfo("OnTrackedImagesChanged");

        foreach(ARTrackedImage i in args.added)
        {
            // a new image tracked
            LogInfo("args.added:" + i.ToString() + ", name:" + i.referenceImage.name + ", imageName ");
            var desList = triggers.FindAll(item => item.imageName == i.referenceImage.name);
            if (desList != null && !desList.IsEmpty())
            {
                trackedImages.Add(i);
                StartCoroutine(AlignModelToTrackedImage(desList, i));
                LogInfo("args.added:" + i.ToString());
            }
        }

        foreach (ARTrackedImage i in args.updated)
        {
            // an image's information is updated.
            // do nothing.
            LogInfo("args.updated:" + i.ToString());
        }

        foreach (ARTrackedImage i in args.removed)
        {
            // an image is no longer tracked.
            trackedImages.Remove(i);
            LogInfo("args.removed:" + i.ToString());
        }
    }

    public void EnableTrackImage()
    {
        trackedImageManager.SetTrackablesActive(true);
        trackedImageManager.enabled = true;
        // 取到一个可修改的 runtime library
        if (trackedImageManager.referenceLibrary is MutableRuntimeReferenceImageLibrary mutableLib)
        {
            runtimeLibrary = mutableLib;
        }
        else
        {
            runtimeLibrary = trackedImageManager.CreateRuntimeLibrary() as MutableRuntimeReferenceImageLibrary;
            trackedImageManager.referenceLibrary = runtimeLibrary;
        }
    }

    public void DisableTrackImage()
    {
        trackedImageManager.SetTrackablesActive(false);
        trackedImageManager.enabled = false;
    }


    private void LogInfo(string msg)
    {
        Debug.Log(msg);
    }

    private void OnDestroy()
    {
        // todo.
    }
}
