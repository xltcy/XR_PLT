using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TrackingImageManager : BaseController
{
    public ARTrackedImageManager trackedImageManager;

    public List<ARTrackedImage> trackedImages = new List<ARTrackedImage>();
    // trackedImageManager's referenceLibrary
    private MutableRuntimeReferenceImageLibrary runtimeLibrary;

    private List<ActionTriggerData> triggers = new List<ActionTriggerData>();

    // Start is called before the first frame update
    void Start()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
            Debug.Log("trackedImageManager注册");
        }    
        //DisableTrackImage();
    }

    // Update is called once per frame
    void Update()
    {

    }

    /**
     * Init ImageRecognition trigger list.
     */
    public void InitTriggeredImage(List<ActionTriggerData> triggerList)
    {
        triggers.Clear();
        triggers.AddRange(triggerList);
        if (triggers.IsEmpty())
        {
            return;
        }
        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.LogError("System not support ARTrackedImageManager");
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

    /**
     * Wait AddReferenceImageJobState complete.
     */
    private IEnumerator CheckJob(AddReferenceImageJobState job, string name)
    {
        while (!job.jobHandle.IsCompleted)
            yield return null;

        job.jobHandle.Complete();

        if (job.status == AddReferenceImageJobStatus.Success)
            Debug.Log($"? Added image {name} successfully!");
        else
            Debug.LogError($"? Failed to add image {name}: {job.status}");

        CheckSetup();

    }

    /**
     * Use to debug trackedImageManager's relating state.
     */
    private void CheckSetup()
    {
        var debugMode = true;
        if (debugMode)
        {
            // 检查管理器
            if (trackedImageManager == null)
            {
                Debug.LogError("ARTrackedImageManager is null!");
                return;
            }
            
            // 检查是否启用
            if (!trackedImageManager.enabled)
            {
                Debug.LogWarning("ARTrackedImageManager is disabled!");
            }
            
            // 检查参考图像库
            if (trackedImageManager.referenceLibrary == null)
            {
                Debug.LogError("Reference Image Library is null!");
            }
            else
            {
                Debug.Log($"Reference Library has {trackedImageManager.referenceLibrary.count} images");
            }
            
            // 检查AR会话
            var arSession = FindObjectOfType<ARSession>();
            if (arSession == null)
            {
                Debug.LogError("ARSession not found in scene!");
            }
        }
    }

    /**
     * Delegate trigger after trackedImage inited.
     */
    IEnumerator DelegateTriggers(List<ActionTriggerData> triggers, ARTrackedImage trackedImage)
    {
        yield return new WaitUntil(() =>
        {
            return trackedImage.transform.position.sqrMagnitude > float.Epsilon;
        });
        triggers.ForEach(trigger => ControllerRefer.SceneController.ConsoleImageRecognizeTrigger(trigger, trackedImage));
    }

    // use to console when a image is tracked.
    public void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach(ARTrackedImage i in args.added)
        {
            // a new image tracked
            var desTriggerList = triggers.FindAll(item => item.imageName == i.referenceImage.name);
            if (desTriggerList != null && !desTriggerList.IsEmpty())
            {
                LogInfo("args.added:" + i.ToString() + ", name:" + i.referenceImage.name + ", imageName " + desTriggerList);
                trackedImages.Add(i);
                StartCoroutine(DelegateTriggers(desTriggerList, i));
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

    /**
     * library init must before enabled = true;
     */
    public void EnableTrackImage()
    {
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
        trackedImageManager.SetTrackablesActive(true);
        trackedImageManager.enabled = true;
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
