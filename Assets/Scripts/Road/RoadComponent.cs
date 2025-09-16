using System.Collections.Generic;
using UnityEngine;

public class RoadComponent : MonoBehaviour
{
    [SerializeField] private float width;
    [SerializeField] private List<Vector3> centerline = new();
    [SerializeField] private Vector3 dirStart;
    [SerializeField] private Vector3 dirEnd;

    private readonly List<GameObject> _chunks = new();

    public List<Vector3> Centerline => centerline;
    public List<GameObject> Chunks => _chunks;

    public void Initialize(List<Vector3> line, Vector3 startDir, Vector3 endDir, float w)
    {
        width = w;
        centerline = new List<Vector3>(line);
        dirStart = startDir;
        dirEnd = endDir;
    }

    public void AddChunk(GameObject go, List<Vector3> chunkCenterline)
    {
        _chunks.Add(go);
    }

    public void RemoveChunk(GameObject go)
    {
        _chunks.Remove(go);
        // NOTE: centerline는 원본 기록용으로 유지(간단 구현).
        // 필요 시 살아있는 chunk들의 중심선을 재구성하도록 확장 가능.
    }

    private void OnDrawGizmos()
    {
        if (centerline == null || centerline.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < centerline.Count - 1; i++)
        {
            Gizmos.DrawLine(transform.TransformPoint(centerline[i]),
                            transform.TransformPoint(centerline[i + 1]));
        }

        // endpoint tangents
        Gizmos.color = Color.green;
        if (centerline.Count >= 1)
        {
            var a = transform.TransformPoint(centerline[0]);
            Gizmos.DrawLine(a, a + dirStart.normalized * (width * 1.5f));
        }
        Gizmos.color = Color.red;
        if (centerline.Count >= 2)
        {
            var b = transform.TransformPoint(centerline[centerline.Count - 1]);
            Gizmos.DrawLine(b, b + dirEnd.normalized * (width * 1.5f));
        }
    }
}
