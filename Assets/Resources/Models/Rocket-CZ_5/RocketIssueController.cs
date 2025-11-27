using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RocketIssueController : MonoBehaviour
{
    #region PublicVar
    public GameObject _rocket;
    public Animator _animator;
    #endregion

    private bool initiateDirty = true;
    private void InitiateTreeNode()
    {
        var treeNodeList = transform.GetComponentsInChildren<ModelTreeNode>();
        foreach (var node in treeNodeList)
        {
            node.InitPlane(transform.rotation, transform.localScale.x);
        }
    }

    //仅做参数解析示例，无实际用处
    class HandleExplosionParam
    {
        public int testParam1;
        public string testParam2;
    }
    
    //在test-SJS中调用这个方法
    public void HandleExplosion(bool isStartAction, string paramJson)
    {
        //仅做参数解析示例，无实际用处
        var param = JsonUtility.FromJson<HandleExplosionParam>(paramJson);
        
        if (initiateDirty)
        {
            InitiateTreeNode();
            initiateDirty = false;
        }
        if (isStartAction)
        {
            Issue_Stage1();
        }
        else
        {
            Reset_Issue_Stage1();
        }
    }
    
    #region Stage1

    private const string AnimStage1 = "Stage1";

    /// <summary>
    /// 一阶段发射
    /// Step 1. 3dof explosion
    /// Step 2. Call animation "Stage1"
    /// </summary>
    public void Issue_Stage1()
    {
        PlayAnim(_animator, AnimStage1);
    }
    
    public void Reset_Issue_Stage1()
    {
        ResetAnim(_animator,  AnimStage1, true);
    }

    private void ResetAnim(Animator animator, string animName, bool isBackward = false)
    {
        if (!animator)
        {
            return;
        }
        
        if (isBackward)
        {
            animator.SetFloat("Speed", -1f);
            animator.Play(animName, -1, 1);
            animator.Update(0f);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
            animator.Play(animName, -1, 0);
            animator.Update(0f);
        }

    }
    
    private void PlayAnim(Animator animator, string animName)
    {
        if (!animator)
        {
            return;
        }
        
        animator.SetFloat("Speed", 1f);
        animator.Play(animName, -1, 0f);
    }
    #endregion


    #region 动画调用
    /// <summary>
    /// 仅供动画调用
    /// </summary>
    private void AnimExplosion()
    {
        var speed = _animator.GetFloat("Speed");
        if (speed > 0f)
        {
            ModelTreeNode.ThreeDofExplosion(_rocket);
        }
        else
        {
            ModelTreeNode.ThreeDofRecovery(_rocket);
        }
    }
    #endregion
}