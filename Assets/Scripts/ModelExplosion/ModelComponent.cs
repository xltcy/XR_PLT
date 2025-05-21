using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelComponent : MonoBehaviour
{
    #region Interface
    /// <summary>
    /// ����mesh�Ķ������꣬���㵱ǰģ�͵�����λ��
    /// </summary>
    /// <returns>��ǰģ�͵�����λ��</returns>
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
                    tmp += transform.TransformPoint(vertex); // ת������������
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
        clickableObject.objIntroduction = "���Ե��" + this.name;

        FindObjectOfType<Click3DObjectManager>().AddClickableObjs(clickableObject);*/
    }
}
