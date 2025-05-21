using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelComponent : MonoBehaviour
{
    #region Interface
    /// <summary>
    /// 根据mesh的顶点坐标，计算当前模型的中心位置
    /// </summary>
    /// <returns>当前模型的中心位置</returns>
    public Vector3 CalculateCenter()
    {
        // Debug.Log(transform.name + " " + transform.childCount);
        Vector3 center = Vector3.zero;
        for (int i = 0; i <  transform.childCount; i ++ )
        {
            Vector3 tmp = Vector3.zero;
            MeshFilter meshFilter = transform.GetChild(i).GetComponent<MeshFilter>();

            if (meshFilter != null)
            {
                Vector3[] vertices = meshFilter.mesh.vertices;

                foreach (Vector3 vertex in vertices)
                {
                    tmp += transform.TransformPoint(vertex); // 转换到世界坐标
                }

                tmp /= vertices.Length;
                center += tmp;
            }
        }
        return center / transform.childCount;
    }
    #endregion

    void Update()
    {

    }

    void Start()
    {
/*        MeshCollider collider = gameObject.AddComponent<MeshCollider>();
        collider.convex = true;
        collider.isTrigger = true;
        collider.sharedMesh = GetComponentInChildren<MeshFilter>().sharedMesh;

        ClickableObject clickableObject = gameObject.AddComponent<ClickableObject>();
        // TBD.
        clickableObject.objIntroduction = "测试点击" + this.name;

        FindObjectOfType<Click3DObjectManager>().AddClickableObjs(clickableObject);*/
    }
}
