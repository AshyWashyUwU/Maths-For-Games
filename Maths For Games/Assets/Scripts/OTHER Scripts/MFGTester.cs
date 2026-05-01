using UnityEngine;

public class MFGTester : MonoBehaviour
{
    private void Start()
    {
        Debug.Log(CustomMathsLibrary.RotateAroundAxis(new Vector3(1, 0, 0), new Vector3(0.2673f, 0.5345f, 0.8018f), CustomMathsLibrary.DegreesToRadians(60)));
    }
}
