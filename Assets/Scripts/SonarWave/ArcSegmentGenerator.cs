using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ArcSegmentGenerator : MonoBehaviour
{
    public GameObject wave_generator;
    public float ori_radius = 1.0f;
    public float thickness = 0.03f;
    public float distance = 0.5f;
    public int count = 4;
    Vector3 emission_position = new Vector3();
    float radius;
    float angle = 15.0f;
    float startAngle = 0f;
    public float centerAngle = 0.0f;
    int segments = 15;
    float timer = 0f;
    float timeRound = 2.0f;
    float angleStep;
    public int in_object = 0;
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
            point += emission_position;
            Matrix4x4 matrix = Matrix4x4.TRS(parentTransform.position, parentTransform.rotation, Vector3.one);
            point = matrix.MultiplyPoint3x4(point);
            lr.SetPosition(i, point);
        }
    }
    void Update()
    {
        allColliders = FindObjectsOfType<BoxCollider>();
        if (timer <= timeRound * count)
        {
            timer += Time.deltaTime;
            float d_Radius = (Time.deltaTime / timeRound) * distance;
            radius += d_Radius;
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Deg2Rad * (startAngle + i * angleStep);
                Vector3 point = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                point += emission_position;
                Matrix4x4 matrix = Matrix4x4.TRS(parentTransform.position, parentTransform.rotation, Vector3.one);
                point = matrix.MultiplyPoint3x4(point);
                if (in_object == 0)
                {
                    foreach (var col in allColliders)
                    {
                        if (col.ClosestPoint(point) == point)
                        {
                            Debug.Log("collider:"+ col.gameObject);
                            WaveReflector waveReflectorScript = col.gameObject.GetComponent<WaveReflector>();
                            Debug.Log("reflector" + waveReflectorScript);
                            if (waveReflectorScript != null)
                            {
                                waveReflectorScript.wave_generator = wave_generator;
                                Debug.Log("111111:" + waveReflectorScript.wave_generator);
                                waveReflectorScript.emission_position = point - col.gameObject.transform.position;
                                Debug.Log(point);
                                Debug.Log(col.gameObject.transform.position);
                                waveReflectorScript.is_on = 1;
                                waveReflectorScript.received_wave = gameObject;
                            }

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
    public void SetEmissionPosition(Vector3 v)
    {
        emission_position = v;
    }
}

