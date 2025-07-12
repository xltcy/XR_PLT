using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UniGLTF;
public class SceneLoader : MonoBehaviour
{
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

    public Transform sceneParent;
    public GameObject defaultModelPrefab;

    void Start()
    {
        LoadSceneFromJson();
    }

    void LoadSceneFromJson()
    {
        string path = Application.dataPath + "/ExportedScenes/scene.json";
        if (!File.Exists(path))
        {
            Debug.LogError("找不到 scene.json！");
            return;
        }

        string json = File.ReadAllText(path);
        SceneData data = JsonUtility.FromJson<SceneData>(json);

        foreach (var obj in data.objects)
        {
            Debug.Log(Application.dataPath + "/ExportedScenes/" + obj.name);
            var bytes = File.ReadAllBytes(Application.dataPath + "/ExportedScenes/" + obj.name);
            var context = new ImporterContext();
            context.ParseGlb(bytes);
            context.Load();
            context.ShowMeshes();
            var model = context.Root;

            if (model == null)
            {
                Debug.LogError("模型加载失败，model 为 null");
                return;
            }

            Vector3 pos = ArrToVec(obj.position);
            Vector3 rot = ArrToVec(obj.rotation);
            Vector3 scale = ArrToVec(obj.scale);

            model.transform.position = pos;
            model.transform.eulerAngles = rot;
            model.transform.localScale = scale;
            model.name = obj.name;

            Debug.Log($"加载模型 {obj.name} 完成");
        }

        // 可视化起点终点
        GameObject start = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        start.transform.position = ArrToVec(data.startPoint);
        start.transform.localScale = Vector3.one * 0.3f;
        start.GetComponent<Renderer>().material.color = Color.green;
        start.name = "起点";

        GameObject end = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        end.transform.position = ArrToVec(data.endPoint);
        end.transform.localScale = Vector3.one * 0.3f;
        end.GetComponent<Renderer>().material.color = Color.red;
        end.name = "终点";
    }

    Vector3 ArrToVec(float[] arr) => new Vector3(arr[0], arr[1], arr[2]);

}
