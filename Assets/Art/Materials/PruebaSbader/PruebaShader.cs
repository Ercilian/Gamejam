using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ToonVertexShadow : MonoBehaviour
{
    public Vector3 lightDirection = new Vector3(0.3f, 1f, 0.5f);

    [Range(0f, 1f)]
    public float shadowValue = 0.6f;

    [Range(0f, 1f)]
    public float cutoff = 0.5f;

    Mesh mesh;
    Color[] colors;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        colors = new Color[mesh.vertexCount];

        ApplyShadow();
    }

    void ApplyShadow()
    {
        Vector3 dir = lightDirection.normalized;

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 normal = mesh.normals[i];
            float d = Vector3.Dot(normal, dir);

            float shade = d > cutoff ? 1f : shadowValue;
            colors[i] = new Color(shade, shade, shade, 1f);
        }

        mesh.colors = colors;
    }
}
