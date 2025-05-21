using System;
using System.IO;
using UnityEngine;

namespace Config
{
    public class ResourceConfig
    {
        private readonly string path;
        private readonly string alternative;
        private string content = null;
        private bool initialized = false;
        private string DiskPath => Path.Combine(Application.persistentDataPath, path);

        public ResourceConfig(string path, string alternative)
        {
            this.path = path;
            this.alternative = alternative;
        }

        public string Content => GetContent();

        //private string GetContent()
        //{
        //    if (!initialized)
        //    {
        //        initialized = true;
        //        try
        //        {
        //            var diskPath = DiskPath;
        //            Debug.Log($"[{nameof(ResourceConfig)}]: Load {diskPath}");
        //            if (File.Exists(diskPath))
        //            {
        //                Debug.Log($"[{nameof(ResourceConfig)}]: Loaded.");
        //                content = File.ReadAllText(diskPath);
        //            }
        //            else
        //            {
        //                content = Resources.Load<TextAsset>(path).text;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            content = alternative;
        //            Debug.LogError($"[{nameof(ResourceConfig)}]: Failed loading: {path} {ex}");
        //        }
        //    }

        //    return content;
        //}
        private string GetContent() {
            return "zh-CN";
        }
    }
}
