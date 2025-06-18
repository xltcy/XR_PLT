using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ArcSegmentGenerator : MonoBehaviour
{
    GameObject wave_generator;
    public float ori_radius = 1.0f;
    public float thickness = 0.03f;
    public float distance = 0.5f;
    public int count = 4;
    float radius;
    float angle = 60.0f;
    float startAngle = 0f;
    public float centerAngle = 0.0f; 
    int segments = 60;
    float timer = 0f;
    float timeRound = 2.0f;
    float angleStep;
    int in_object = 0;
    Collider[] allColliders;
    Transform parentTransform;
    LineRenderer lr;
    void Start()
    {
        radius = ori_radius;
        parentTransform = transform.parent;
        lr = GetComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.widthMultiplier = thickness;
        angleStep = angle / segments;
        startAngle = centerAngle - angle / 2;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * (startAngle + i * angleStep);
            Vector3 point = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            Matrix4x4 matrix = Matrix4x4.TRS(parentTransform.position, parentTransform.rotation, Vector3.one);
            point =  matrix.MultiplyPoint3x4(point);
            lr.SetPosition(i, point);
        }
    }
    void Update()
    {
        allColliders = FindObjectsOfType<Collider>();
        if (timer <= timeRound * count)
        {
            timer += Time.deltaTime;
            float d_Radius = (Time.deltaTime / timeRound) * distance;
            radius += d_Radius;
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Deg2Rad * (startAngle + i * angleStep);
                Vector3 point = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Matrix4x4 matrix = Matrix4x4.TRS(parentTransform.position, parentTransform.rotation, Vector3.one);
                point = matrix.MultiplyPoint3x4(point);
                if(in_object == 0)
                {
                    foreach (var col in allColliders)
                    {
                        if (col.ClosestPoint(point) == point)
                        {
                            Debug.Log(11);
                            WaveReflector waveReflectorScript = col.gameObject.GetComponent<WaveReflector>();
                            waveReflectorScript.is_on = 1;
                            waveReflectorScript.wave_generator = wave_generator;
                            in_object = 1;
                            break;
                        }
                    }
                }
                lr.SetPosition(i, point);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void SetWaveGenerator(GameObject v)
    {
        wave_generator = v;
    }
}

