using UnityEngine;

public class Cube : MonoBehaviour
{
    [Header("Angles (degrees per second)")]
    public float yawSpeedDeg = 90f;
    public float pitchSpeedDeg = 30f;

    [Header("Movement")]
    public float moveSpeed = 2f;

    [Header("Scale")]
    public Vector3 nonUniformScale = Vector3.one;

    private Mesh mesh;
    private Vector3[] originalVerts;
    private Vector3[] deformedVerts;

    private float yawRad;
    private float pitchRad;
    private Vector3 P;

    private void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVerts = mesh.vertices;
        deformedVerts = new Vector3[originalVerts.Length];

        yawRad = 0f;
        pitchRad = 0f;

        P = new Vector3(0, 0, 0); //Just making it 0 for this workshop otherwise Unity overrides it
    }

    private void Update()
    {
        // 1) Update yaw/pitch (radians)
        yawRad = yawRad + MFGCore.DegreesToRadians(yawSpeedDeg) * Time.deltaTime;
        pitchRad = pitchRad + MFGCore.DegreesToRadians(pitchSpeedDeg) * Time.deltaTime;

        // 2) Build forward from angles (your existing function from earlier weeks)
        Vector3 F = MFGCore.ForwardFromYawPitch(yawRad, pitchRad);

        // 3) Translate our position along forward
        P = MFGCore.Add(P, MFGCore.Scale(F, moveSpeed * Time.deltaTime));

        // 4) TODO: Make a function call to build basis from forward (R,U,F)

        MFGCore.Vector3 r;
        MFGCore.Vector3 u;
        MFGCore.Vector3 f;

        MFGCore.BuildBasisFromForward(F, out r, out u, out f);

        // 5) TODO: Reassign R, U, and F to Bake non-uniform scale into basis BEFORE packing our 4x4 Matrix
        r = MFGCore.NormalFromScale(r, nonUniformScale);
        u = MFGCore.NormalFromScale(u, nonUniformScale);
        f = MFGCore.NormalFromScale(f, nonUniformScale);

        // 6) TODO: Build Matrix4 from (R,U,F,P)
        MFGCore.Matrix4 m = new MFGCore.Matrix4(r, u, f, P);

        for (int i = 0; i < originalVerts.Length; i++)
        {
            // TODO (B): Use your TransformPoint helper to transform deformedVerts[i]
            deformedVerts[i] = MFGCore.TransformPoint(m, originalVerts[i]);
        }

        // 8) Apply back to mesh

        mesh.vertices = deformedVerts;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

}

