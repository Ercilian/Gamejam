using System.Collections;
using UnityEngine;

public class DissolvingController : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMesh;
    public float dissolveRate = 0.0125f;
    public float refreshRate = 0.025f;
    private Material[] skinnedMaterials;

    void Start()
    {
        if (skinnedMesh != null)
        {
            skinnedMaterials = skinnedMesh.materials;
        }
        StartCoroutine(DissolveCall());
    }


    // void Update() eliminado para evitar múltiples llamadas a la coroutine

    IEnumerator DissolveCo()
    {
        if(skinnedMaterials.Length > 0)
        {
            float counter = 0f;
            while (skinnedMaterials[0].GetFloat("_DissolveAmount") < 1)
            {
                counter += dissolveRate;
                for(int i = 0; i < skinnedMaterials.Length; i++)
                {
                    skinnedMaterials[i].SetFloat("_DissolveAmount", counter);
                }
                yield return new WaitForSeconds(refreshRate);
            }
        }
    }

    IEnumerator DissolveCall()
    {
        yield return new WaitForSeconds(3f);
        StartCoroutine(DissolveCo());
    }
}
