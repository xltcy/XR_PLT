using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class ModelTreeNode : MonoBehaviour
{
    #region SerializeVar
    [Header("��ע")]
    public string ��ը��ʽ;
    [Header("Identity")]
    // ��Ǹýڵ��Ƿ�ΪҶ�ӽ��
    public bool _isLeafNode;
    // ��Ǹýڵ��Ƿ�Ϊģ���еĸ��ڵ�
    public bool _isRoot;
    // frame��ʾ�ڲ㼶��ը�в������仯�����
    public bool _isFrame;
    [Header("Child")]
    public List<GameObject> _children;
    [Header("Property")]
    public float _mass;
    public Vector3 _standardAxis;
    public float _standardIntensity;    // ÿ���ӽڵ㱬ըʱ�ƶ��ľ���
    public float _factor;       // ��ǰ�ڵ㱬ըʱ�ƶ������Ȩ��
    #endregion

    #region PrivateVar
    public Vector3 _center;
    private Vector3 _direction;
    private float _deltaIntensity;
    private float _time;
    private float _deltaTime;
    [SerializeField]
    private Vector3 _axis;
    private float _intensity;
    #endregion

    #region Interface

    public Vector3 center 
    { 
        get { return _center; } 
    }

    /// <summary>
    /// ��ʼ���ɻ���λ�úͱ�ը����
    /// </summary>
    /// <param name="rotation">��Ӧ�ɻ����ڵ�transform.rotation����ֵ</param>
    /// <param name="scale">��Ӧ�ɻ����ڵ�transform.localScale����ֵ</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitPlane(Quaternion rotation, float scale)
    {
        CalculateCenter();
        InitExplosionProperty(rotation, scale);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThreeDofExplosion(GameObject parent)
    {
        ModelTreeNode parentNode = parent.GetComponent<ModelTreeNode>();
        parentNode.NaiveExplosion();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TwoDofExplosion(GameObject parent)
    {
        ModelTreeNode parentNode = parent.GetComponent<ModelTreeNode>();
        parentNode.SurfaceAlignedExplosion();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OneDofExplosion(GameObject parent)
    {
        ModelTreeNode parentNode = parent.GetComponent<ModelTreeNode>();
        parentNode.AxisAlignedExplosion();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThreeDofRecovery(GameObject parent)
    {
        ModelTreeNode parentNode = parent.GetComponent<ModelTreeNode>();
        parentNode.NaiveExplosion(recovery:true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TwoDofRecovery(GameObject parent)
    {
        ModelTreeNode parentNode = parent.GetComponent<ModelTreeNode>();
        parentNode.SurfaceAlignedExplosion(recovery:true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OneDofRecovery(GameObject parent)
    {
        ModelTreeNode parentNode = parent.GetComponent<ModelTreeNode>();
        parentNode.AxisAlignedExplosion(recovery:true);
    }
    #endregion

    #region Initialize
    /// <summary>
    /// ���ݡ���ʼ�㡱�͡�Ŀ��㡱����ģ�ͣ�ʹ������õ�����ʼ�㡱��λ�ã�������������Ŀ��㡱��
    /// </summary>
    /// <param name="model">��Ҫ���õ�ģ��</param>
    /// <param name="initPosition">��ʼ���λ��</param>
    /// <param name="targetPosition">Ŀ����λ��</param>
    /// <param name="forward">ģ�ͳ�ǰ�ķ���</param>
    static public void InitTransformByTarget(GameObject model, Vector3 initPosition, Vector3 targetPosition, Vector3 forward)
    {
        var rotation = Quaternion.LookRotation(forward, targetPosition - initPosition);
        model.transform.position = initPosition;
        model.transform.rotation = rotation;
    }

    /// <summary>
    /// ���㵱ǰ�ڵ������λ�ã������浽����_center��
    /// </summary>
    private void CalculateCenter()
    {
        // �����ǰ�ڵ���Ҷ�ӽڵ㣬��ģ�͵����ļ�Ϊ��ǰ�ڵ������
        if (_isLeafNode == true)
        {
            // Debug.Log("[tcluan Debug] " + name + " calcualates center.\n");
            _center = _children[0].GetComponent<ModelComponent>().CalculateCenter();
        }
        // ���򣬵�ǰ�ڵ�Ϊ�ڲ��ڵ㣬��ģ�͵�����Ϊ������ӽڵ������
        else
        {
            float totalMass = 0.0f;
            _center = Vector3.zero;
            foreach (GameObject child in _children)
            {
                ModelTreeNode childNode = child.GetComponent<ModelTreeNode>();
                childNode.CalculateCenter();
                _center += childNode.center * childNode._mass;
                totalMass += childNode._mass;
            }
            _center /= totalMass;
        }
        // Debug.Log("[tcluan Debug] " + name + " center: " + _center);
    }

    /// <summary>
    /// ����ģ�͵���ת�����ţ����·ɻ��ı�ը����ͱ�ը���� 
    /// </summary>
    /// <param name="rotation">��Ӧtransform.rotation�ĸ���</param>
    /// <param name="scale">��Ӧtransform.scale�ĸ���</param>
    private void InitExplosionProperty(Quaternion rotation, float scale)
    {
        if (_isLeafNode == true)
        {
            _axis = rotation * _standardAxis;
            _intensity = scale * _standardIntensity;
        }
        else
        {
            _axis = rotation * _standardAxis;
            _intensity = scale * _standardIntensity;
            foreach (GameObject child in _children)
            {
                ModelTreeNode childNode = child.GetComponent<ModelTreeNode>();
                childNode.InitExplosionProperty(rotation, scale);
            }
        }
    }
    #endregion

    #region Explosion

        /// <summary>
        /// ģ�����Ÿ������򣬰�ָ�������ƶ�
        /// </summary>
        /// <param name="direction">�ƶ�����</param>
        /// <param name="intensity">�ƶ�����</param>
    private void ModelMovement(Vector3 direction, float intensity)
    {
        _direction = direction;
        _deltaIntensity = intensity * _factor * _deltaTime;
        _time += 1.0f;
        CenterMovement(direction, intensity);
    }

    /// <summary>
    /// �ݹ��ƶ������ӽڵ������λ��
    /// </summary>
    /// <param name="direction">�ƶ�����</param>
    /// <param name="intensity">�ƶ�����</param>
    private void CenterMovement(Vector3 direction, float intensity)
    {
        _center += direction * _factor * intensity;
        if (_isLeafNode == true)
        {
            return;
        }
        foreach (GameObject child in _children)
        {
            ModelTreeNode childNode = child.GetComponent<ModelTreeNode>();
            childNode.CenterMovement(direction, intensity);
        }
    }

    /// <summary>
    /// ����ָ�������ӽڵ㱬ը��
    /// ���У���ը�����ɸ��ڵ�����ĵ㣨center��������
    /// </summary>
    private void NaiveExplosion(bool recovery=false)
    {
        //Debug.Log("[tcluan Debug] Explode is trigged.\n");
        foreach (GameObject child in _children)
        {
            ModelTreeNode childNode = child.GetComponent<ModelTreeNode>();
            // frame�ڵ��ڱ�ըʱ���ƶ�
            if (childNode._isFrame == true)
            {
                continue;
            }
            // ��ը����Ϊ�Ӹ��ڵ�ָ���ӽڵ�ķ���
            Vector3 direction = (childNode.center - _center).normalized;
            childNode.ModelMovement(direction, (recovery==true)?-_intensity:_intensity);
        }
    }

    /// <summary>
    /// ���������ӽڵ㡾ƽ���ڸ���ƽ�桿��ը��
    /// ���Ը��ڵ㱬ը���ߣ�explosionAxis��Ϊ���ߵ�ƽ�档
    /// </summary>
    private void SurfaceAlignedExplosion(bool recovery=false)
    {
        //Debug.Log("[tcluan Debug] Explode is trigged.\n");
        foreach (GameObject child in _children)
        {
            ModelTreeNode childNode = child.GetComponent<ModelTreeNode>();
            // frame�ڵ��ڱ�ըʱ���ƶ�
            if (childNode._isFrame == true)
            {
                continue;
            }
            // vecParent2Child�ķ���Ϊ�Ӹ��ڵ�ָ���ӽڵ�ķ���
            Vector3 vecParent2Child = childNode.center - _center,
                // vecAlongNormalΪ����vecParent2Child��ƽ�淨�߷����ϵ�ͶӰ��
                vecAlongNormal = Vector3.Dot(vecParent2Child, _axis) * _axis;
            // ��ը����ƽ���ڸ���ƽ�棬����ֱ��ƽ��ķ��ߣ��༴�ڷ��߷����ϵķ���Ϊ0��
            // ��˰��ط��߷��������vecParent2Child��ȥ�����ɡ�
            Vector3 direction = (vecParent2Child - vecAlongNormal).normalized;
            //Debug.Log("[tcluan Debug] " + !recovery + " " + name + " dirction: " + direction + " intensity: " + _intensity);
            childNode.ModelMovement(direction, (recovery==true)?-_intensity:_intensity);
        }
    }
    
    /// <summary>
    /// ����ָ�����ڵ���һ�㼶�������ӽڵ㡾ƽ���ڸ������ߡ���ը��
    /// ���У���ը���������ɸ��ڵ�ı�ը���ߣ�explosionAxis�����������
    /// ��ը����������ɸ��ڵ����ӽڵ�����ڱ�ը���ߵ�λ�ù�ϵ������
    /// </summary>
    private void AxisAlignedExplosion(bool recovery=false)
    {
        //Debug.Log("[tcluan Debug] Explode is trigged.\n");
        foreach (GameObject child in _children)
        {
            ModelTreeNode childNode = child.GetComponent<ModelTreeNode>();
            // frame�ڵ��ڱ�ըʱ���ƶ�
            if (childNode._isFrame == true)
            {
                continue;
            }
            // ��ըƽ���ڸ��ڵ�ı�ը����
            Vector3 direction = _axis;
            // ��ը����������ɸ����ӽڵ��ڱ�ը���ߵ�λ�ù�ϵ����
            if (Vector3.Dot(childNode.center, _axis) < Vector3.Dot(_center, _axis))
            {
                direction *= -1.0f;
            }
            // ���ַ���������ķ����������һ�ο����š������ڷ���Ϊ��ʱ��������һ���������ˡ���ע�⣬normalized�൱��һ���������+һ�ο��������㣩
            //// ��ը����ΪvecParent2Chlid�ڱ�ը�����ϵ�ͶӰ
            //Vector3 vecParent2Chlid = childNode.center - _center;
            //Vector3 direction = (Vector3.Dot(vecParent2Chlid, _explosionAxis) * _explosionAxis).normalized;

            childNode.ModelMovement(direction, (recovery==true)?-_intensity:_intensity);
        }
    }
    #endregion

    #region Behaviour
    private void Start()
    {
        if (_isRoot)
        {
            InitPlane(transform.parent.rotation, transform.localScale.x);
            //Debug.Log("[tcluan Debug] Plane explosion axis: " + _explosionAxis);
            //Debug.Log("[tcluan Debug] Plane center: " + _center);
        }
        _deltaTime = 0.05f;
        _axis = _axis.normalized; // �������2024.11.04����������ȷ�������explosionAxis���ǵ�λ����������Ҫɾ������
    }

    private void FixedUpdate()
    {
        if (_time > 0)
        {
            _time -= _deltaTime;
            transform.position += _deltaIntensity * _direction;

            if (_time <= 0)
            {
                _time = 0;
                _deltaIntensity = 0;
                _direction = Vector3.zero;
            }
        }
    }
    #endregion

    #region Test
    static public void test(GameObject model)
    {
        InitTransformByTarget(model, new Vector3(1, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
    }
    #endregion
}
