using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Speech2BlendshapeController : MonoBehaviour
{
    public GameObject guideHead;

    private SkinnedMeshRenderer smr;
    private readonly Dictionary<string, int> blendShapeIndexCache = new Dictionary<string, int>();
    private readonly HashSet<string> warnedMissingBlendShapes = new HashSet<string>();

    private Dictionary<uint, string[]> visemeToBlendShape = new Dictionary<uint, string[]>
    {
        { 0u, null },
        { 1u, Names("aa", "viseme_aa") },
        { 2u, Names("aa", "viseme_aa") },
        { 3u, Names("oh", "viseme_oh", "O") },
        { 4u, Names("E", "viseme_E", "ee") },
        { 5u, Names("RR", "viseme_RR", "R") },
        { 6u, Names("ih", "viseme_ih") },
        { 7u, Names("oh", "viseme_oh", "O") },
        { 8u, Names("oh", "viseme_oh", "O") },
        { 9u, Names("oh", "viseme_oh", "O") },
        { 10u, Names("oh", "viseme_oh", "O") },
        { 11u, Names("aa", "viseme_aa") },
        { 12u, Names("mouthOpen", "viseme_mouthOpen", "jawOpen", "aa") },
        { 13u, Names("RR", "viseme_RR", "R") },
        { 14u, Names("mouthFunnel", "viseme_mouthFunnel", "funnel", "oh") },
        { 15u, Names("SS", "viseme_SS", "S") },
        { 16u, Names("CH", "viseme_CH") },
        { 17u, Names("TH", "viseme_TH") },
        { 18u, Names("FF", "viseme_FF", "F") },
        { 19u, Names("DD", "viseme_DD", "D") },
        { 20u, Names("kk", "viseme_kk", "K") },
        { 21u, Names("PP", "viseme_PP", "P") },
    };

    #region Unity生命周期

    private void Start()
    {
        SetGuideHead(guideHead);
    }

    #endregion

    #region 公开接口

    /// <summary>
    /// 获取 Azure visemeId 默认对应的 BlendShape 名称。
    /// </summary>
    public string GetBlendshapeName(uint i)
    {
        if (!visemeToBlendShape.TryGetValue(i, out string[] names) || names == null || names.Length == 0)
        {
            return null;
        }

        return names[0];
    }

    /// <summary>
    /// 根据 Azure 返回的 visemeId 设置口型 BlendShape 权重。
    /// 会先清空当前模型所有 BlendShape，再按别名缓存查找目标口型。
    /// </summary>
    public void SetVisemeBlendShapeWeight(uint visemeId, float weight)
    {
        if (!smr || !smr.sharedMesh)
        {
            return;
        }

        ResetAllBlendShapes();

        if (!visemeToBlendShape.TryGetValue(visemeId, out string[] blendShapeNames) || blendShapeNames == null)
        {
            return;
        }

        int index = GetBlendShapeIndex(blendShapeNames);
        if (index < 0)
        {
            string warningKey = blendShapeNames.Length > 0 ? blendShapeNames[0] : visemeId.ToString();
            if (warnedMissingBlendShapes.Add(warningKey))
            {
                Debug.LogWarning($"BlendShape for visemeId {visemeId} was not found. Expected aliases: {string.Join(", ", blendShapeNames)}");
            }

            return;
        }

        smr.SetBlendShapeWeight(index, weight);
    }

    /// <summary>
    /// 清空当前模型上所有 BlendShape 权重，避免上一个口型残留。
    /// </summary>
    public void ResetAllBlendShapes()
    {
        if (!smr || !smr.sharedMesh)
        {
            return;
        }

        for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
        {
            smr.SetBlendShapeWeight(i, 0f);
        }
    }

    /// <summary>
    /// 绑定新的头部模型，并重建 BlendShape 名称到索引的缓存。
    /// 如果传入节点上没有 SkinnedMeshRenderer，会继续在子节点中查找。
    /// </summary>
    public void SetGuideHead(GameObject head)
    {
        if (!head)
        {
            return;
        }

        guideHead = head;
        smr = guideHead.GetComponent<SkinnedMeshRenderer>();
        if (!smr)
        {
            smr = guideHead.GetComponentInChildren<SkinnedMeshRenderer>();
        }

        RebuildBlendShapeCache();
    }

    #endregion

    #region BlendShape索引缓存

    /// <summary>
    /// 按候选名称查找 BlendShape 索引。
    /// 先精确匹配，再使用归一化后的别名缓存匹配。
    /// </summary>
    private int GetBlendShapeIndex(string[] candidateNames)
    {
        foreach (string candidateName in candidateNames)
        {
            if (string.IsNullOrEmpty(candidateName)) continue;

            int exactIndex = smr.sharedMesh.GetBlendShapeIndex(candidateName);
            if (exactIndex >= 0)
            {
                return exactIndex;
            }

            string normalizedName = NormalizeBlendShapeName(candidateName);
            if (blendShapeIndexCache.TryGetValue(normalizedName, out int cachedIndex))
            {
                return cachedIndex;
            }
        }

        return -1;
    }

    /// <summary>
    /// 扫描当前模型的全部 BlendShape 名称，建立归一化名称到索引的缓存。
    /// 例如模型里叫 viseme_RR 时，会同时缓存 viseme_RR 和 RR 两种别名。
    /// </summary>
    private void RebuildBlendShapeCache()
    {
        blendShapeIndexCache.Clear();
        warnedMissingBlendShapes.Clear();

        if (!smr || !smr.sharedMesh)
        {
            return;
        }

        for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
        {
            string rawName = smr.sharedMesh.GetBlendShapeName(i);
            AddBlendShapeAlias(rawName, i);

            const string visemePrefix = "viseme_";
            if (rawName.StartsWith(visemePrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                AddBlendShapeAlias(rawName.Substring(visemePrefix.Length), i);
            }
        }
    }

    /// <summary>
    /// 添加一个 BlendShape 名称别名到缓存。
    /// 同名冲突时保留第一个索引，避免覆盖模型原始顺序。
    /// </summary>
    private void AddBlendShapeAlias(string name, int index)
    {
        string normalizedName = NormalizeBlendShapeName(name);
        if (!string.IsNullOrEmpty(normalizedName) && !blendShapeIndexCache.ContainsKey(normalizedName))
        {
            blendShapeIndexCache.Add(normalizedName, index);
        }
    }

    /// <summary>
    /// 将 BlendShape 名称归一化：忽略大小写、下划线、空格等非字母数字字符。
    /// 例如 viseme_RR、Viseme RR 和 viseme-RR 都会变成 visemerr。
    /// </summary>
    private static string NormalizeBlendShapeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(name.Length);
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// 用于声明一个 visemeId 可匹配的多个 BlendShape 名称别名。
    /// </summary>
    private static string[] Names(params string[] names)
    {
        return names;
    }

    #endregion
}
