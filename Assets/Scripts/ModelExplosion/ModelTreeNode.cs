using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class ModelTreeNode : MonoBehaviour
{
    #region SerializeVar
    [Header("备注")]
    public string 爆炸方式;
    [Header("Identity")]
    // 标记该节点是否为叶子结点
    public bool _isLeafNode;
    // 标记该节点是否为模型中的根节点
    public bool _isRoot;
    // frame表示在层级爆炸中不发生变化的组件
    public bool _isFrame;
    [Header("Child")]
    public List<GameObject> _children;
    [Header("Property")]
    public float _mass;
    public Vector3 _standardAxis;
    public float _standardIntensity;    // 每个子节点爆炸时移动的距离
    public float _factor;       // 当前节点爆炸时移动距离的权重
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
    /// 初始化飞机的位置和爆炸参数
    /// </summary>
    /// <param name="rotation">对应飞机父节点transform.rotation的数值</param>
    /// <param name="scale">对应飞机父节点transform.localScale的数值</param>
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
    /// 根据“初始点”和“目标点”放置模型，使对象放置到“初始点”的位置，并且正方向朝向“目标点”。
    /// </summary>
    /// <param name="model">需要放置的模型</param>
    /// <param name="initPosition">初始点的位置</param>
    /// <param name="targetPosition">目标点的位置</param>
    /// <param name="forward">模型朝前的方向</param>
    static public void InitTransformByTarget(GameObject model, Vector3 initPosition, Vector3 targetPosition, Vector3 forward)
    {
        var rotation = Quaternion.LookRotation(forward, targetPosition - initPosition);
        model.transform.position = initPosition;
        model.transform.rotation = rotation;
    }

    /// <summary>
    /// 计算当前节点的中心位置，并保存到变量_center中
    /// </summary>
    private void CalculateCenter()
    {
        // 如果当前节点是叶子节点，则模型的中心即为当前节点的中心
        if (_isLeafNode == true)
        {
            // Debug.Log("[tcluan Debug] " + name + " calcualates center.\n");
            _center = _children[0].GetComponent<ModelComponent>().CalculateCenter();
        }
        // 否则，当前节点为内部节点，则模型的中心为其各个子节点的中心
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
    /// 根据模型的旋转与缩放，更新飞机的爆炸方向和爆炸距离 
    /// </summary>
    /// <param name="rotation">对应transform.rotation的更改</param>
    /// <param name="scale">对应transform.scale的更改</param>
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
        /// 模型沿着给定方向，按指定距离移动
        /// </summary>
        /// <param name="direction">移动方向</param>
        /// <param name="intensity">移动距离</param>
    private void ModelMovement(Vector3 direction, float intensity)
    {
        _direction = direction;
        _deltaIntensity = intensity * _factor * _deltaTime;
        _time += 1.0f;
        CenterMovement(direction, intensity);
    }

    /// <summary>
    /// 递归移动所有子节点的中心位置
    /// </summary>
    /// <param name="direction">移动方向</param>
    /// <param name="intensity">移动距离</param>
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
    /// 控制指定所有子节点爆炸。
    /// 其中，爆炸中心由父节点的中心点（center）决定。
    /// </summary>
    private void NaiveExplosion(bool recovery=false)
    {
        //Debug.Log("[tcluan Debug] Explode is trigged.\n");
        foreach (GameObject child in _children)
        {
            ModelTreeNode childNode = child.GetComponent<ModelTreeNode>();
            // frame节点在爆炸时不移动
            if (childNode._isFrame == true)
            {
                continue;
            }
            // 爆炸方向为从父节点指向子节点的方向
            Vector3 direction = (childNode.center - _center).normalized;
            childNode.ModelMovement(direction, (recovery==true)?-_intensity:_intensity);
        }
    }

    /// <summary>
    /// 控制所有子节点【平行于给定平面】爆炸。
    /// 且以父节点爆炸轴线（explosionAxis）为法线的平面。
    /// </summary>
    private void SurfaceAlignedExplosion(bool recovery=false)
    {
        //Debug.Log("[tcluan Debug] Explode is trigged.\n");
        foreach (GameObject child in _children)
        {
            ModelTreeNode childNode = child.GetComponent<ModelTreeNode>();
            // frame节点在爆炸时不移动
            if (childNode._isFrame == true)
            {
                continue;
            }
            // vecParent2Child的方向为从父节点指向子节点的方向。
            Vector3 vecParent2Child = childNode.center - _center,
                // vecAlongNormal为向量vecParent2Child在平面法线方向上的投影。
                vecAlongNormal = Vector3.Dot(vecParent2Child, _axis) * _axis;
            // 爆炸方向平行于给定平面，即垂直于平面的法线，亦即在法线方向上的分量为0。
            // 因此把沿法线方向分量从vecParent2Child中去除即可。
            Vector3 direction = (vecParent2Child - vecAlongNormal).normalized;
            //Debug.Log("[tcluan Debug] " + !recovery + " " + name + " dirction: " + direction + " intensity: " + _intensity);
            childNode.ModelMovement(direction, (recovery==true)?-_intensity:_intensity);
        }
    }
    
    /// <summary>
    /// 控制指定父节点下一层级的所有子节点【平行于给定轴线】爆炸。
    /// 其中，爆炸的正方向由父节点的爆炸轴线（explosionAxis）方向决定；
    /// 爆炸方向的正负由父节点与子节点相对于爆炸轴线的位置关系决定。
    /// </summary>
    private void AxisAlignedExplosion(bool recovery=false)
    {
        //Debug.Log("[tcluan Debug] Explode is trigged.\n");
        foreach (GameObject child in _children)
        {
            ModelTreeNode childNode = child.GetComponent<ModelTreeNode>();
            // frame节点在爆炸时不移动
            if (childNode._isFrame == true)
            {
                continue;
            }
            // 爆炸平行于父节点的爆炸轴线
            Vector3 direction = _axis;
            // 爆炸方向的正负由父，子节点在爆炸轴线的位置关系决定
            if (Vector3.Dot(childNode.center, _axis) < Vector3.Dot(_center, _axis))
            {
                direction *= -1.0f;
            }
            // 这种方法比上面的方法多计算了一次开根号。并且在方向为正时还会多计算一次向量数乘。（注意，normalized相当于一次向量点积+一次开根号运算）
            //// 爆炸方向为vecParent2Chlid在爆炸轴线上的投影
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
        _axis = _axis.normalized; // 【栾天驰2024.11.04留】除非能确定填入的explosionAxis都是单位向量，否则不要删掉这行
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
