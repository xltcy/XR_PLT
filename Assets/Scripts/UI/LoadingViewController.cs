using Unity.VectorGraphics;
using UnityEngine;

public class LoadingViewController : MonoBehaviour
{
    public float loadingRotationSpeed = 200f; // 加载旋转速度

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // 加载旋转动画
        GetComponent<SVGImage>().transform.Rotate(0, 0, -loadingRotationSpeed * Time.deltaTime);
    }
}
