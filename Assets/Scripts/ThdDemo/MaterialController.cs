using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialController : BaseController
{
    public Material _transparent_material;
    public bool _enableDebug = true;

    private Dictionary<MeshRenderer, Material[]> _originalMaterials = new Dictionary<MeshRenderer, Material[]>();

    public void SetTransparent(GameObject target)
    {
        ProcessGameObject(target);
    }

    public void RestoreMaterials()
    {
        foreach (var kvp in _originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.materials = kvp.Value;
            }
        }
    }

    public void RestoreMaterials(MeshRenderer renderer)
    {
        if (_originalMaterials.ContainsKey(renderer)) renderer.materials = _originalMaterials[renderer];
    }


    private void ProcessGameObject(GameObject target)
    {
        if (target.transform.childCount == 0) return;

        foreach(Transform child in target.transform)
        {
            var meshRenderer = child.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null) {
                ReplaceAllMaterials(meshRenderer);
            }

            ProcessGameObject(child.gameObject);
        }
    }


    public void ReplaceAllMaterials(MeshRenderer renderer)
    {
        if (renderer == null || _transparent_material == null)
            return;

        BackupMaterials(renderer);

        // �����µĲ�������
        Material[] newMaterials = new Material[renderer.materials.Length];

        for (int i = 0; i < newMaterials.Length; i++)
        {
            newMaterials[i] = _transparent_material;
        }

        // Ӧ���²���
        renderer.materials = newMaterials;

        if (_enableDebug)
        {
            Debug.Log($"�ѽ� {renderer.gameObject.name} �� {newMaterials.Length} �������滻Ϊ͸������");
        }
    }

    private void BackupMaterials(MeshRenderer renderer)
    {
        if (!_originalMaterials.ContainsKey(renderer))
        {
            _originalMaterials[renderer] = renderer.materials;
        }
    }
}
