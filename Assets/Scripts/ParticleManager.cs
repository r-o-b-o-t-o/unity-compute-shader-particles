using UnityEngine;

public class ParticleManager : MonoBehaviour
{
#pragma warning disable 0649
    private struct Particle
    {
        public Vector3 position;
        public Vector3 prevPos;
        public Vector3 acceleration;
        public Vector3 velocity;
        public float health;
        public float decayRate;
        public Color color;
    }

    private struct Box
    {
        public Vector3 minimum;
        public Vector3 maximum;
    }

    private struct Plane
    {
        public Vector3 p;
        public Vector3 n;
    }

    [SerializeField] private ProceduralSphereGenerator sphereGenerator;
#pragma warning restore 0649

    public ComputeShader computeShader;
    public Material material;
    public Transform[] planeObjects;
    public Transform[] boxObjects;

    private Particle[] particles;
    private ComputeBuffer particlesBuffer;
    private Box[] boxes;
    private ComputeBuffer boxesBuffer;
    private Plane[] planes;
    private ComputeBuffer planesBuffer;
    private Matrix4x4[] transforms;
    private int kernelIndex;
    private int seedPropertyId;
    private int dtPropertyId;
    private Mesh particleMesh;
    private MaterialPropertyBlock materialPropertyBlock;
    private float[] sizes;
    private Vector4[] colors;

    private void Start()
    {
        int n = 1023;

        this.particles = new Particle[n];
        this.particlesBuffer = new ComputeBuffer(n, 72);

        if (this.boxObjects.Length > 0)
        {
            this.boxes = new Box[this.boxObjects.Length];
            this.UpdateBoxes();
            this.boxesBuffer = new ComputeBuffer(this.boxes.Length, 24);
            this.boxesBuffer.SetData(this.boxes);
        }

        if (this.planeObjects.Length > 0)
        {
            this.planes = new Plane[this.planeObjects.Length];
            this.UpdatePlanes();
            this.planesBuffer = new ComputeBuffer(this.planes.Length, 24);
            this.planesBuffer.SetData(this.planes);
        }

        this.transforms = new Matrix4x4[n];
        for (int i = 0; i < this.transforms.Length; ++i)
        {
            this.transforms[i] = new Matrix4x4();
        }

        this.kernelIndex = this.computeShader.FindKernel("UpdateParticle");
        this.computeShader.SetBuffer(this.kernelIndex, "particles", this.particlesBuffer);
        if (this.boxObjects.Length > 0)
        {
            this.computeShader.SetBuffer(this.kernelIndex, "boxes", this.boxesBuffer);
        }
        if (this.planeObjects.Length > 0)
        {
            this.computeShader.SetBuffer(this.kernelIndex, "planes", this.planesBuffer);
        }
        this.seedPropertyId = Shader.PropertyToID("randSeed");
        this.dtPropertyId = Shader.PropertyToID("deltaTime");

        this.particleMesh = this.sphereGenerator.GetMesh();

        this.materialPropertyBlock = new MaterialPropertyBlock();
        this.sizes = new float[n];
        this.colors = new Vector4[n];
    }

    private bool UpdateBoxes()
    {
        bool needsUpdate = false;
        for (int i = 0; i < this.boxObjects.Length; i++)
        {
            if (this.boxObjects[i].hasChanged)
            {
                Bounds b = new Bounds(this.boxObjects[i].position, this.boxObjects[i].localScale);
                this.boxes[i].minimum = b.min;
                this.boxes[i].maximum = b.max;
                needsUpdate = true;
            }
        }
        return needsUpdate;
    }

    private bool UpdatePlanes()
    {
        bool needsUpdate = false;
        for (int i = 0; i < this.planeObjects.Length; ++i)
        {
            if (this.planeObjects[i].hasChanged)
            {
                this.planes[i].p = this.planeObjects[i].position;
                this.planes[i].n = this.planeObjects[i].up;
                this.planeObjects[i].hasChanged = false;
                needsUpdate = true;
            }
        }
        return needsUpdate;
    }

    private void Update()
    {
        // Update boxes
        if (this.UpdateBoxes())
        {
            this.boxesBuffer.SetData(this.boxes);
        }

        // Update planes
        if (this.UpdatePlanes())
        {
            this.planesBuffer.SetData(this.planes);
        }

        // Update properties and run kernel
        this.computeShader.SetFloat(this.seedPropertyId, Random.Range(1.0f, 10000.0f));
        this.computeShader.SetFloat(this.dtPropertyId, Time.deltaTime);
        this.computeShader.Dispatch(this.kernelIndex, 2, 2, 1);
        this.particlesBuffer.GetData(this.particles);

        int n = this.particles.Length;
        for (int i = 0; i < n; ++i)
        {
            // Fill our transform matrices
            Particle p = this.particles[i];
            this.transforms[i].SetTRS(p.position + this.transform.position, Quaternion.identity, Vector3.one);

            this.sizes[i] = this.particles[i].health + 0.5f;
            this.colors[i] = this.particles[i].color;
        }

        this.materialPropertyBlock.SetFloatArray("_Size", this.sizes);
        this.materialPropertyBlock.SetVectorArray("_Color", this.colors);

        // Draw the particles
        Graphics.DrawMeshInstanced(this.particleMesh, 0, this.material, this.transforms, n, this.materialPropertyBlock);
    }

    private void OnDestroy()
    {
        this.particlesBuffer.Dispose();
        if (this.boxesBuffer != null)
        {
            this.boxesBuffer.Dispose();
        }
        if (this.planesBuffer != null)
        {
            this.planesBuffer.Dispose();
        }
    }
}
