using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _Test : MonoBehaviour
{
    public GameObject _snoar;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // ��J���Ա�ըЧ��
        if (Input.GetKeyDown(KeyCode.J))
        {
            // �������д���ִ�б�ը
            ModelTreeNode.OneDofExplosion(_snoar);
            // ��ը����ͨ��Prefab-Snoar/snoar �������Standard Intensity������������ƣ�������===================================>��
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            // �������д���ִ�б�ը
            ModelTreeNode.OneDofRecovery(_snoar);
            // ��ը����ͨ��Prefab-Snoar/snoar �������Standard Intensity������������ƣ�������===================================>��
        }
    }
}
