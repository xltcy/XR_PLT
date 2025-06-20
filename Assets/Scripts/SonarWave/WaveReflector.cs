using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveReflector : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject wave;
    public int is_on = 0;
    public float distance = 0.3f;
    public float timeRound = 1.5f;
    public float ori_radius = 0.65f;
    public float centerAngle = 0.0f;
    public GameObject wave_generator;
    public int count = 5;
    int index = 0;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (index >= count) index = 0;
        if (is_on == 1)
        {
            
            Vector3 aPos = transform.position; 
            Vector3 bPos = wave_generator.transform.position;
            Vector3 dirA = new Vector3(aPos.x, 0, aPos.z);
            Vector3 dirB = new Vector3(bPos.x, 0, bPos.z);
            Vector3 vec = dirB - dirA;
            Vector3 right = new Vector3(transform.right.x, 0, transform.right.z);

            float angle = Vector3.SignedAngle(vec, right, Vector3.up);
            Debug.Log(angle);
            Debug.Log(wave_generator);
            GameObject child = Instantiate(wave);
            ArcSegmentGenerator waveScript = child.GetComponent<ArcSegmentGenerator>();
            waveScript.ori_radius = ori_radius;
            waveScript.distance = distance;
            waveScript.count = count;
            waveScript.centerAngle = angle;
            child.transform.SetParent(transform);
            child.name = "wave" + index;
            index += 1;
            is_on = 0;
        }
    }
}
