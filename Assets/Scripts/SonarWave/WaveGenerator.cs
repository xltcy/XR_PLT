using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveGenerator : MonoBehaviour
{
    public GameObject wave;
    public float distance = 0.3f;
    public float timeRound = 1.5f;
    public float ori_radius = 0.65f;
    public int count = 5;
    float timer = 0f;
    int index = 0;
    bool generate_on = false;
    // Start is called before the first frame update
    void Start()
    {
        //StartGenerate();
    }
    public void StartGenerate()
    {
        generate_on = true;
    }
    public void StopGenerate()
    {
        generate_on = false;
    }
    // Update is called once per frame
    void Update()
    {
        if (generate_on)
        {
            if (index >= count) index = 0;
            //if (index >= count) return;
            timer += Time.deltaTime;
            if (timer >= timeRound)
            {
                GameObject child = Instantiate(wave);
                ArcSegmentGenerator waveScript = child.GetComponent<ArcSegmentGenerator>();
                waveScript.ori_radius = ori_radius;
                waveScript.distance = distance;
                waveScript.count = count;
                waveScript.centerAngle = 90.0f;
                waveScript.SetWaveGenerator(gameObject);
                child.transform.SetParent(transform);
                child.name = "wave" + index;
                timer = 0;
                index += 1;
            }
        }

    }
}
