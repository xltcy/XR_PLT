using UnityEngine;
using System.Collections.Generic;
using System.IO;


public class StudioExporter : MonoBehaviour
{
    // public StudioManager studio;

    [System.Serializable]
    public class SceneObject
    {
        public string name;
        public string glb;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public string description;
        public string audioFile;
    }

    [System.Serializable]
    public class SceneData
    {
        public float[] startPoint;
        public float[] endPoint;
        public List<SceneObject> objects;
    }

    public void Export()
    {
        string path = Application.dataPath + "/ExportedScenes/";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        //var data = new SceneData
        //{
        //    startPoint = VecToArr(studio.startPoint),
        //    endPoint = VecToArr(studio.endPoint),
        //    objects = new List<SceneObject>()
        //};

        //foreach (var obj in studio.placedModels)
        //{
        //    Debug.Log("name:" + obj.name);
        //    var meta = obj.GetComponent<ModelMetadata>();
        //    data.objects.Add(new SceneObject
        //    {
        //        name = obj.name,
        //        glb = meta.modelFileName,
        //        position = VecToArr(obj.transform.position),
        //        rotation = VecToArr(obj.transform.eulerAngles),
        //        scale = VecToArr(obj.transform.localScale),
        //        description = meta.description,
        //        audioFile = meta.audioFileName,
        //    });
        //}

        //string json = JsonUtility.ToJson(data, true);
        //File.WriteAllText(path + "scene.json", json);
        Debug.Log("导出成功：" + path + "scene.json");
    }

    float[] VecToArr(Vector3 v) => new float[] { v.x, v.y, v.z };
}
