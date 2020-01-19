using UnityEngine;

public class ProceduralSphereGenerator : MonoBehaviour
{
    public ComputeShader computeShader;

    private Mesh generatedMesh;

    private void Start()
    {
        float radius = 0.2f;
        int nbLong = 24;
        int nbLat = 16;

        int kernelIndex = this.computeShader.FindKernel("SphereGenerator");

        Vector3[] vertices = new Vector3[(nbLong + 1) * nbLat + 2];
        ComputeBuffer verticesBuffer = new ComputeBuffer(vertices.Length, 12);
        this.computeShader.SetBuffer(kernelIndex, "vertices", verticesBuffer);

        Vector3[] normals = new Vector3[vertices.Length];
        ComputeBuffer normalsBuffer = new ComputeBuffer(normals.Length, 12);
        this.computeShader.SetBuffer(kernelIndex, "normals", normalsBuffer);

        this.computeShader.Dispatch(kernelIndex, 1, 1, 1);

        verticesBuffer.GetData(vertices);
        normalsBuffer.GetData(normals);

        vertices[0] = Vector3.up * radius;
        normals[0] = vertices[0].normalized;
        vertices[vertices.Length - 1] = Vector3.down * radius;
        normals[vertices.Length - 1] = vertices[vertices.Length - 1].normalized;

        verticesBuffer.Dispose();
        normalsBuffer.Dispose();

        this.generatedMesh = new Mesh();

        int nbFaces = vertices.Length;
        int nbTriangles = nbFaces * 2;
        int nbIndexes = nbTriangles * 3;
        int[] triangles = new int[nbIndexes];

        //Top Cap
        int i = 0;
        for (int lon = 0; lon < nbLong; lon++)
        {
            triangles[i++] = lon + 2;
            triangles[i++] = lon + 1;
            triangles[i++] = 0;
        }

        //Middle
        for (int lat = 0; lat < nbLat - 1; lat++)
        {
            for (int lon = 0; lon < nbLong; lon++)
            {
                int current = lon + lat * (nbLong + 1) + 1;
                int next = current + nbLong + 1;

                triangles[i++] = current;
                triangles[i++] = current + 1;
                triangles[i++] = next + 1;

                triangles[i++] = current;
                triangles[i++] = next + 1;
                triangles[i++] = next;
            }
        }

        //Bottom Cap
        for (int lon = 0; lon < nbLong; lon++)
        {
            triangles[i++] = vertices.Length - 1;
            triangles[i++] = vertices.Length - (lon + 2) - 1;
            triangles[i++] = vertices.Length - (lon + 1) - 1;
        }

        this.generatedMesh.vertices = vertices;
        this.generatedMesh.normals = normals;
        this.generatedMesh.triangles = triangles;

        this.generatedMesh.RecalculateBounds();
        this.generatedMesh.Optimize();
    }

    public Mesh GetMesh()
    {
        return this.generatedMesh;
    }
}
