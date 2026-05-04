using UnityEngine;

[ExecuteAlways]
public class WorldVisualizer : MonoBehaviour
{
    private float pinHeight = 2f;

    private void OnDrawGizmos()
    {
        float halfWidth = WorldData.laneWidth;
        float depth = WorldData.laneDepth;
        float groundY = WorldData.worldGroundPos;

        Vector3 bl = new Vector3(-halfWidth, groundY, 0);
        Vector3 br = new Vector3(halfWidth, groundY, 0);
        Vector3 tl = new Vector3(-halfWidth, groundY, depth);
        Vector3 tr = new Vector3(halfWidth, groundY, depth);

        float topY = groundY + pinHeight;

        Vector3 blTop = new Vector3(bl.x, topY, bl.z);
        Vector3 brTop = new Vector3(br.x, topY, br.z);
        Vector3 tlTop = new Vector3(tl.x, topY, tl.z);
        Vector3 trTop = new Vector3(tr.x, topY, tr.z);

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);

        Gizmos.DrawLine(blTop, brTop);
        Gizmos.DrawLine(brTop, trTop);
        Gizmos.DrawLine(trTop, tlTop);
        Gizmos.DrawLine(tlTop, blTop);

        Gizmos.DrawLine(bl, blTop);
        Gizmos.DrawLine(br, brTop);
        Gizmos.DrawLine(tl, tlTop);
        Gizmos.DrawLine(tr, trTop);
    }
}