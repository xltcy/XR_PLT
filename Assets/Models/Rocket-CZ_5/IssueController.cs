using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IssueController : MonoBehaviour
{
    #region PublicVar
    public GameObject _rocket;
    public Animator _animator;
    #endregion

    #region Stage1
    /// <summary>
    /// Ò»½×¶Î·¢Éä
    /// Step 1. 3dof explosion
    /// Step 2. Call animation "Stage1"
    /// </summary>
    public void Issue_Stage1()
    {
        ModelTreeNode.ThreeDofExplosion(_rocket);
        _animator.Play("Stage1");
    }
    #endregion
}
