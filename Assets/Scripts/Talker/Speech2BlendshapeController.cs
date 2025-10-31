using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Speech2BlendshapeController : BaseController
{
    public GameObject guideHead;
    private SkinnedMeshRenderer smr;

    private Dictionary<uint, string> visemeToBlendShape = new Dictionary<uint, string>
    {
        { 0u, null },
        { 1u, "aa" },
        { 2u, "aa" },
        { 3u, "oh" },
        { 4u, "E" },
        { 5u, "RR" },
        { 6u, "ih" },
        { 7u, "oh" },
        { 8u, "oh" },
        { 9u, "oh" },
        { 10u, "oh" },
        { 11u, "aa" },
        { 12u, "mouthOpen" },
        { 13u, "RR" },
        { 14u, "mouthFunnel" },
        { 15u, "SS" },
        { 16u, "CH" },
        { 17u, "TH" },
        { 18u, "FF" },
        { 19u, "DD" },
        { 20u, "kk" },
        { 21u, "PP" },
    };

    public string GetBlendshapeName(uint i)
    {
        return this.visemeToBlendShape[i];
    }

    public void SetVisemeBlendShapeWeight(uint visemeId, float weight)
    {
        ResetAllBlendShapes();
        //if (!visemeToBlendShape.TryGetValue(visemeId, out string blendShapeName))
        //{
        //    Debug.LogWarning($"No blendshape mapped for visemeId {visemeId}");
        //    return;
        //}
        string blendShapeName = GetBlendshapeName(visemeId);

        if (string.IsNullOrEmpty(blendShapeName))
        {
            // 对应blendshape为空，可能不需要动作
            return;
        }

        // 找blendshape名字对应的索引
        int index = smr.sharedMesh.GetBlendShapeIndex(blendShapeName);
        if (index < 0)
        {
            Debug.LogWarning($"BlendShape '{blendShapeName}' not found in mesh.");
            return;
        }

        smr.SetBlendShapeWeight(index, weight);
    }

    public void ResetAllBlendShapes()
    {
        for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
        {
            smr.SetBlendShapeWeight(i, 0f);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        smr = guideHead.GetComponent<SkinnedMeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
