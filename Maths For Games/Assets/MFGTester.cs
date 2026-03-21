using UnityEngine;

public class MFGTester : MonoBehaviour
{
    private void Start()
    {
        Debug.Log(MFGCore.Dot4(new Vector4(1.2f, 0.0f, -0.5f, 6.0f), new Vector4(2.0f, 1.0f, -4.0f, 1.0f)));

        Debug.Log(MFGCore.ForwardFromYawPitch(MFGCore.DegreesToRadians(30), MFGCore.DegreesToRadians(20)));

        Debug.Log(MFGCore.Normalize(MFGCore.CrossProduct(new Vector3(0, 1f, 0), new Vector3(0.4698463f, 0.3420201f, 0.8137977f))));

        Debug.Log("---------------------- Stuff ----------------------");

        Vector3 scalar = new Vector3(2, 1.5f, 0.5f);

        Vector3 r = MFGCore.Scale(new Vector3(0.8660254f, 0, -0.5f), 2);
        Vector3 u = MFGCore.Scale(new Vector3(0, 1, 0), 1.5f);
        Vector3 f = MFGCore.Scale(new Vector3(0.4698463f, 0.3420201f, 0.8137977f), 0.5f);
        Vector3 p = new Vector3(4, -2, 7);

        MFGCore.Matrix4 m = new MFGCore.Matrix4(r, u, f, p);

        Debug.Log("---------------------- PACKING ----------------------");

        Debug.Log(m.row0);
        Debug.Log(m.row1);
        Debug.Log(m.row2);
        Debug.Log(m.row3);

        Debug.Log("---------------------- COMBINING WITH POINT / DIRECTION ----------------------");

        Vector4 point = new Vector4(1, 0, 2, 1);
        Vector4 direction = new Vector4(1, 0, 2, 0);

        Debug.Log(m.Multiply(point));

        Debug.Log(m.Multiply(direction));
    }
}
