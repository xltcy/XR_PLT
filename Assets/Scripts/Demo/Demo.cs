using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TreeEditor;
using UnityEngine;

public class Demo : MonoBehaviour
{
    public GameObject Ì«ºÍµî, ÎÝ¶¥, ÉÏÎÝ¶¥, ÏÂÎÝ¶¥, ÎÝÉí;

    private void demo()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Alpha1))ModelTreeNode.OneDofExplosion(Ì«ºÍµî);
        if(Input.GetKeyUp(KeyCode.Alpha2))ModelTreeNode.TwoDofExplosion(ÎÝ¶¥);
        if(Input.GetKeyUp(KeyCode.Alpha3))ModelTreeNode.ThreeDofExplosion(ÉÏÎÝ¶¥);
        if(Input.GetKeyUp(KeyCode.Alpha4))ModelTreeNode.ThreeDofExplosion(ÏÂÎÝ¶¥);
        if(Input.GetKeyUp(KeyCode.Alpha5))ModelTreeNode.TwoDofExplosion(ÎÝÉí);

    }
}
