using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Click-Click Road Builder with snapping:
/// - 1st LMB: set start (snaps to nearest road if within snapDistance)
/// - Move mouse: preview updates with snapping to end as well
/// - 2nd LMB: build road (segmented). Stores endpoint directions in Road component
/// - SHIFT/ALT: force straight line (ignore curves/right-angle rule)
/// - If snapping to the middle of a road, approach perpendicularly (right angle)
/// - RMB during preview: cancel road
/// - RMB idle: delete clicked chunk only
/// </summary>
[DefaultExecutionOrder(-10)]
public class RoadBuilder : MonoBehaviour
{
    [Header("Raycast / Input")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float rayMaxDistance = 2000f;

    [Header("Appearance")]
    [SerializeField] private float roadWidth = 3f;
    [SerializeField] private Material roadMaterial;
    [SerializeField] private float uvTilingPerMeter = 1f;

    [Header("Curve")]
    [SerializeField] private float samplesPerMeter = 1.5f;
    [SerializeField] private float handleLenRatio = 0.25f;
    [SerializeField] private float maxHandleLen = 8f;

    [Header("Segmentation / Snapping")]
    [SerializeField] private float segmentLength = 6f;
    [SerializeField] private float snapDistance = 2.0f;
    [SerializeField] private Vector3 up = default; // (0,0,0) → Vector3.up

    [Header("Preview")]
    [SerializeField] private Color previewColor = new Color(1f, 0.85f, 0.2f, 0.9f);

    [SerializeField] private bool _buildModeEnabled = true;
    public bool BuildModeEnabled
    {
        get => _buildModeEnabled;
        set
        {
            if (_buildModeEnabled == value) return;
            _buildModeEnabled = value;
            if (!_buildModeEnabled && _isPreviewing)
            {
                _isPreviewing = false;
                if (_previewGO) _previewGO.SetActive(false);
            }
        }
    }

    private bool _initialized;
    private Camera _cam;
    private Transform _roadsParent;
    private Transform _previewParent;
    private GameObject _previewGO;
    private MeshFilter _previewMF;
    private MeshRenderer _previewMR;

    // click-then-click
    private bool _isPreviewing;
    private Vector3 _startAnchor;
    private bool _startSnapped;
    private Vector3 _startSnapTangent;
    private bool _startSnapIsEndpoint;

    // registries
    private readonly List<RoadComponent> _roads = new();            // for snapping
    private readonly List<RoadChunkRef> _chunks = new();    // for deletion

    private void Awake() => InitOnce();

    private void InitOnce()
    {
        if (_initialized) return;
        _initialized = true;

        _cam = Camera.main;
        if (up == Vector3.zero) up = Vector3.up;

        if (!roadMaterial)
        {
            roadMaterial = new Material(Shader.Find("Standard"));
            roadMaterial.enableInstancing = true;
        }

        _roadsParent = new GameObject("Roads").transform;
        _roadsParent.SetParent(transform, false);

        _previewParent = new GameObject("RoadPreview").transform;
        _previewParent.SetParent(transform, false);

        _previewGO = new GameObject("Preview");
        _previewGO.transform.SetParent(_previewParent, false);
        _previewMF = _previewGO.AddComponent<MeshFilter>();
        _previewMR = _previewGO.AddComponent<MeshRenderer>();
        _previewMR.sharedMaterial = roadMaterial;
        _previewMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _previewMR.receiveShadows = false;
        SetMaterialColor(_previewMR, previewColor);
        _previewGO.SetActive(false);
    }

    private void Update()
    {
        if (!_cam) _cam = Camera.main;

        // 건설 모드 ON일 때만 좌클릭(시작/완료)과 프리뷰 갱신 동작
        if (BuildModeEnabled)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!_isPreviewing)
                {
                    if (RayToGround(out var hitPos))
                    {
                        _startSnapped = TryFindSnap(hitPos, out var sPoint, out var sTan, out var sIsEnd);
                        if (_startSnapped)
                        { _startAnchor = sPoint; _startSnapTangent = sTan; _startSnapIsEndpoint = sIsEnd; }
                        else
                        { _startAnchor = hitPos; _startSnapTangent = Vector3.zero; _startSnapIsEndpoint = false; }

                        _isPreviewing = true;
                        _previewGO.SetActive(true);
                    }
                }
                else
                {
                    if (RayToGround(out var endPos))
                    {
                        bool straight = IsStraightMode();
                        var centerline = BuildCenterlineWithSnap(_startAnchor, endPos, straight, out _);
                        if (centerline != null && centerline.Count >= 2)
                        {
                            var dirStart = DirectionAtStart(centerline);
                            var dirEnd = DirectionAtEnd(centerline);

                            var roadGO = new GameObject($"Road_{_roads.Count}");
                            roadGO.transform.SetParent(_roadsParent, false);
                            roadGO.layer = gameObject.layer;
                            var road = roadGO.AddComponent<RoadComponent>();
                            road.Initialize(centerline, dirStart, dirEnd, roadWidth);

                            CreateChunksUnderRoad(road, centerline, roadGO.transform);
                            _roads.Add(road);
                        }
                    }
                    _isPreviewing = false;
                    _previewGO.SetActive(false);
                }
            }

            if (_isPreviewing && RayToGround(out var mousePos))
            {
                bool straight = IsStraightMode();
                var centerline = BuildCenterlineWithSnap(_startAnchor, mousePos, straight, out _);
                UpdatePreview(centerline);
            }
        }

        // 우클릭: 프리뷰 중이면 취소, 아니면 청크 삭제 (건설 모드와 무관)
        if (Input.GetMouseButtonDown(1))
        {
            if (_isPreviewing)
            {
                _isPreviewing = false;
                _previewGO.SetActive(false);
            }
            else
            {
                TryDeleteChunkUnderMouse();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && _isPreviewing)
        {
            _isPreviewing = false;
            _previewGO.SetActive(false);
        }
    }


    private bool IsStraightMode()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    }

    private bool RayToGround(out Vector3 pos)
    {
        var ray = _cam ? _cam.ScreenPointToRay(Input.mousePosition) : new Ray();
        if (Physics.Raycast(ray, out var hit, rayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            pos = hit.point;
            return true;
        }
        pos = default;
        return false;
    }

    // Build path, considering start snap (saved in state) and end snap
    private List<Vector3> BuildCenterlineWithSnap(Vector3 start, Vector3 mouseEnd, bool straightMode, out Vector3 endTangentUsed)
    {
        endTangentUsed = Vector3.zero;

        // 종료 스냅
        bool endSnapped = TryFindSnap(mouseEnd, out var ePoint, out var eTan, out var eIsEnd);
        var end = endSnapped ? ePoint : mouseEnd;

        // 시작/종료 힌트
        Vector3 startHint = Vector3.zero;
        Vector3 endHint = Vector3.zero;

        Vector3 abDir = (end - start).sqrMagnitude > 1e-6f ? (end - start).normalized : Vector3.forward;

        // 시작 스냅 처리
        if (_startSnapped)
        {
            if (_startSnapIsEndpoint)
            {
                // 시작점 엔드포인트: 기존 접선을 "동일방향 강제"하지 않고, 기본 접선 사용(필요 시 부호 선택은 아래 규칙과 동일 로직을 써도 됨)
                startHint = AlignToTargetDir(_startSnapTangent, start, end);
            }
            else
            {
                // 시작점 중앙 스냅: 진행방향에 가장 가까운 수직방향 선택
                startHint = ChoosePerpendicular(_startSnapTangent, abDir);
            }
        }

        // 종료 스냅 처리
        if (endSnapped)
        {
            if (eIsEnd)
            {
                // 엔드포인트에 붙을 때, 기존 접선을 '접근 방향(End->Start)'과 같은 방향으로 뒤집지 말고
                // '반대 방향'이 되도록 부호를 선택 → 동일방향으로 미끄러지듯 이어지던 문제 방지
                endHint = AvoidCoDirectional(eTan, start, end);
            }
            else
            {
                // 중앙 스냅: 끝점에서의 접근 방향과 가장 맞는 수직방향으로 유도(직각 접속)
                var approach = abDir; // (end - start).normalized
                endHint = ChoosePerpendicular(eTan, approach);
            }
            endTangentUsed = endHint;
        }

        // 직선 모드이거나 힌트가 전혀 없으면 직선
        if (straightMode || (startHint == Vector3.zero && endHint == Vector3.zero))
            return BuildStraight(start, end);

        // 힌트가 하나라도 있으면 베지어(없는 쪽은 AB방향으로 fallback)
        return BuildBezier(start, end, startHint, endHint);
    }

    // hint(접선 후보)를 from→to 진행 방향과 같은 쪽으로 정렬(부호 선택)
    private static Vector3 AlignToTargetDir(Vector3 hint, Vector3 from, Vector3 to)
    {
        if (hint == Vector3.zero) return Vector3.zero;
        Vector3 d = (to - from);
        if (d.sqrMagnitude < 1e-6f) return hint.normalized;
        return (Vector3.Dot(hint, d) >= 0f) ? hint.normalized : (-hint.normalized);
    }

    // 엔드포인트 접선 hint를 '접근 방향(End->Start)'과 동방향이면 뒤집어서 "반대"가 되도록 정규화
    private static Vector3 AvoidCoDirectional(Vector3 tangentAtEnd, Vector3 start, Vector3 end)
    {
        if (tangentAtEnd == Vector3.zero) return Vector3.zero;
        Vector3 approach = (start - end);
        if (approach.sqrMagnitude < 1e-6f) return tangentAtEnd.normalized;

        // 동일 방향(내적 > 0)이면 반전, 아니면 그대로
        return (Vector3.Dot(tangentAtEnd, approach) > 0f)
            ? (-tangentAtEnd.normalized)
            : (tangentAtEnd.normalized);
    }

    private List<Vector3> BuildStraight(Vector3 a, Vector3 b)
    {
        var line = new List<Vector3>(32);
        float dist = Vector3.Distance(a, b);
        int steps = Mathf.Max(2, Mathf.CeilToInt(dist * Mathf.Max(0.25f, samplesPerMeter)));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            line.Add(Vector3.Lerp(a, b, t));
        }
        return line;
    }

    // roadTangent과 직교하는 두 방향 중, desiredDir과 더 잘 맞는 쪽을 선택
    private Vector3 ChoosePerpendicular(Vector3 roadTangent, Vector3 desiredDir)
    {
        if (roadTangent == Vector3.zero) return Vector3.zero;
        if (desiredDir == Vector3.zero) return Vector3.Cross(up, roadTangent).normalized;

        var perpL = Vector3.Cross(up, roadTangent).normalized; // 왼쪽
        var perpR = -perpL;                                     // 오른쪽

        // desiredDir(원하는 진행/접근 방향)과 더 큰 내적을 갖는 쪽 선택
        return Vector3.Dot(desiredDir.normalized, perpL) >= Vector3.Dot(desiredDir.normalized, perpR)
            ? perpL : perpR;
    }

    private List<Vector3> BuildBezier(Vector3 a, Vector3 b, Vector3 startDirHint, Vector3 endDirHint)
    {
        var line = new List<Vector3>(64);
        Vector3 ab = b - a;
        float d = Mathf.Max(0.001f, ab.magnitude);

        Vector3 h1 = a + (startDirHint == Vector3.zero ? ab.normalized : startDirHint.normalized)
                        * Mathf.Min(d * handleLenRatio, maxHandleLen);

        Vector3 h2 = b - (endDirHint == Vector3.zero ? ab.normalized : endDirHint.normalized)
                        * Mathf.Min(d * handleLenRatio, maxHandleLen);

        int steps = Mathf.Max(6, Mathf.CeilToInt(d * samplesPerMeter));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            line.Add(Bezier(a, h1, h2, b, t));
        }
        return line;
    }

    private static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
    }

    private void UpdatePreview(List<Vector3> centerline)
    {
        if (centerline == null || centerline.Count < 2)
        {
            _previewGO.SetActive(false);
            return;
        }
        _previewGO.SetActive(true);
        var mesh = MeshFromCenterline(centerline, roadWidth, uvTilingPerMeter, up);
        _previewMF.sharedMesh = mesh;
    }

    private void CreateChunksUnderRoad(RoadComponent road, List<Vector3> centerline, Transform parent)
    {
        if (centerline == null || centerline.Count < 2) return;

        float acc = 0f;
        var pts = new List<Vector3> { centerline[0] };

        for (int i = 1; i < centerline.Count; i++)
        {
            float step = Vector3.Distance(centerline[i - 1], centerline[i]);
            if (acc + step < segmentLength)
            {
                pts.Add(centerline[i]);
                acc += step;
                continue;
            }

            float remain = segmentLength - acc;
            Vector3 dir = (centerline[i] - centerline[i - 1]);
            float len = dir.magnitude;

            if (len > 1e-4f)
            {
                Vector3 cut = centerline[i - 1] + dir.normalized * remain;
                pts.Add(cut);
                MakeChunk(road, pts, parent);

                // next seed
                pts.Clear();
                pts.Add(cut);

                float leftover = step - remain;
                acc = 0f;

                while (leftover >= segmentLength)
                {
                    Vector3 cut2 = pts[pts.Count - 1] + dir.normalized * segmentLength;
                    pts.Add(cut2);
                    MakeChunk(road, pts, parent);
                    pts.Clear();
                    pts.Add(cut2);
                    leftover -= segmentLength;
                }

                if (leftover > 0f)
                {
                    Vector3 tail = pts[pts.Count - 1] + dir.normalized * leftover;
                    pts.Add(tail);
                    acc = leftover;
                }
            }
        }

        if (pts.Count >= 2)
            MakeChunk(road, pts, parent);
    }

    private void MakeChunk(RoadComponent road, List<Vector3> centerline, Transform parent)
    {
        var go = new GameObject($"RoadChunk_{road.Chunks.Count}");
        go.transform.SetParent(parent, false);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = roadMaterial;

        var mesh = MeshFromCenterline(centerline, roadWidth, uvTilingPerMeter, up);
        mf.sharedMesh = mesh;

        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = false;

        // keep references
        road.AddChunk(go, new List<Vector3>(centerline));
        _chunks.Add(new RoadChunkRef { go = go, mesh = mesh });
    }

    private void TryDeleteChunkUnderMouse()
    {
        if (!_cam) return;

        var ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, rayMaxDistance, ~0, QueryTriggerInteraction.Collide)) return;

        var go = hit.collider ? hit.collider.gameObject : null;
        if (!go) return;

        // remove from global chunk list
        for (int i = 0; i < _chunks.Count; i++)
        {
            if (_chunks[i].go == go)
            {
                _chunks.RemoveAt(i);
                break;
            }
        }

        // remove from its Road component
        var road = go.GetComponentInParent<RoadComponent>();
        if (road) road.RemoveChunk(go);

        Destroy(go);
    }

    // Snapping ------------------------------------------------------------

    private bool TryFindSnap(Vector3 query, out Vector3 snapPoint, out Vector3 snapTangent, out bool isEndpoint)
    {
        snapPoint = default;
        snapTangent = default;
        isEndpoint = false;

        float bestDist = float.MaxValue;
        bool found = false;

        for (int r = 0; r < _roads.Count; r++)
        {
            var road = _roads[r];
            if (road.Centerline == null || road.Centerline.Count < 2) continue;

            if (ClosestPointOnPolyline(road.Centerline, query, out var p, out var tan, out bool endpoint, out float d))
            {
                if (d < bestDist)
                {
                    bestDist = d;
                    snapPoint = p;
                    snapTangent = tan;
                    isEndpoint = endpoint;
                    found = true;
                }
            }
        }

        return found && bestDist <= snapDistance;
    }

    private static bool ClosestPointOnPolyline(List<Vector3> poly, Vector3 q, out Vector3 pt, out Vector3 tangent, out bool endpoint, out float dist)
    {
        pt = default;
        tangent = Vector3.forward;
        endpoint = false;
        dist = float.MaxValue;
        if (poly == null || poly.Count < 2) return false;

        for (int i = 0; i < poly.Count - 1; i++)
        {
            Vector3 a = poly[i];
            Vector3 b = poly[i + 1];
            Vector3 ab = b - a;
            float ab2 = Vector3.SqrMagnitude(ab);
            if (ab2 < 1e-6f) continue;

            float t = Vector3.Dot(q - a, ab) / ab2;
            t = Mathf.Clamp01(t);
            Vector3 p = a + ab * t;

            float d = Vector3.Distance(q, p);
            if (d < dist)
            {
                dist = d;
                pt = p;
                tangent = ab.normalized;

                // true endpoint if projection exactly near ends or equals first/last
                endpoint = (t <= 1e-3f) || (t >= 1f - 1e-3f) ||
                            (Vector3.Distance(p, poly[0]) < 1e-3f) ||
                            (Vector3.Distance(p, poly[poly.Count - 1]) < 1e-3f);
            }
        }
        return dist < float.MaxValue;
    }

    // Mesh ----------------------------------------------------------------

    private static Mesh MeshFromCenterline(List<Vector3> cl, float width, float uvPerM, Vector3 up)
    {
        int n = cl.Count;
        var verts = new Vector3[n * 2];
        var uvs = new Vector2[n * 2];
        var tris = new int[(n - 1) * 6];

        float half = width * 0.5f;

        float running = 0f;
        for (int i = 0; i < n; i++)
        {
            Vector3 fwd;
            if (i == 0) fwd = (cl[1] - cl[0]).normalized;
            else if (i == n - 1) fwd = (cl[n - 1] - cl[n - 2]).normalized;
            else fwd = (cl[i + 1] - cl[i - 1]).normalized;

            Vector3 left = Vector3.Cross(up, fwd).normalized;

            var pL = cl[i] - left * half;
            var pR = cl[i] + left * half;

            verts[i * 2 + 0] = pL;
            verts[i * 2 + 1] = pR;

            if (i > 0) running += Vector3.Distance(cl[i - 1], cl[i]);
            float v = running * uvPerM;

            uvs[i * 2 + 0] = new Vector2(0, v);
            uvs[i * 2 + 1] = new Vector2(1, v);
        }

        int ti = 0;
        for (int i = 0; i < n - 1; i++)
        {
            int i0 = i * 2;
            int i1 = i * 2 + 1;
            int i2 = i * 2 + 2;
            int i3 = i * 2 + 3;

            tris[ti++] = i0; tris[ti++] = i2; tris[ti++] = i3;
            tris[ti++] = i0; tris[ti++] = i3; tris[ti++] = i1;
        }

        var mesh = new Mesh { name = "RoadRibbon" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void SetMaterialColor(Renderer r, Color c)
    {
        if (r && r.sharedMaterial && r.sharedMaterial.HasProperty("_Color"))
            r.sharedMaterial.color = c;
    }

    private static Vector3 DirectionAtStart(List<Vector3> cl)
    {
        return (cl.Count >= 2 && (cl[1] - cl[0]).sqrMagnitude > 1e-6f)
            ? (cl[1] - cl[0]).normalized : Vector3.forward;
    }

    private static Vector3 DirectionAtEnd(List<Vector3> cl)
    {
        int n = cl.Count;
        return (n >= 2 && (cl[n - 1] - cl[n - 2]).sqrMagnitude > 1e-6f)
            ? (cl[n - 1] - cl[n - 2]).normalized : DirectionAtStart(cl);
    }

    // Data ----------------------------------------------------------------

    private struct RoadChunkRef
    {
        public GameObject go;
        public Mesh mesh;
    }
}
