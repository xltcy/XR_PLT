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
        // 按J测试爆炸效果
        if (Input.GetKeyDown(KeyCode.J))
        {
            // 调用这行代码执行爆炸
            ModelTreeNode.OneDofExplosion(_snoar);
            // 爆炸距离通过Prefab-Snoar/snoar 这个对象，Standard Intensity这个变量来控制（在这里===================================>）
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            // 调用这行代码执行爆炸
            ModelTreeNode.OneDofRecovery(_snoar);
            // 爆炸距离通过Prefab-Snoar/snoar 这个对象，Standard Intensity这个变量来控制（在这里===================================>）
        }
    }
}
