using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hanok
{
    #region PlotVertex Structure

    /// <summary>
    /// 스냅된 점의 라인 타입
    /// </summary>
    [System.Serializable]
    public enum SnapLineType
    {
        LeftEdge,
        RightEdge
    }

    /// <summary>
    /// 개별 스냅 정보
    /// </summary>
    [System.Serializable]
    public struct SnapInfo
    {
        public Vector3 snappedPosition;
        public RoadComponent snappedRoad;
        public SnapLineType snappedLine;

        public SnapInfo(Vector3 position, RoadComponent road, SnapLineType lineType)
        {
            snappedPosition = position;
            snappedRoad = road;
            snappedLine = lineType;
        }
    }

    /// <summary>
    /// Plot vertex information containing position, snap status, and detailed snap information
    /// </summary>
    [System.Serializable]
    public struct PlotVertex
    {
        [SerializeField] public bool isSnapped;
        [SerializeField] public List<SnapInfo> snapInfos;

        public PlotVertex(bool snapped = false, List<SnapInfo> infos = null)
        {
            isSnapped = snapped;
            snapInfos = infos ?? new List<SnapInfo>();
        }

        public Vector3 GetPositionForCurve(RoadComponent targetRoad)
        {
            foreach (var info in snapInfos)
            {
                if (info.snappedRoad == targetRoad)
                {
                    return info.snappedPosition;
                }
            }
            return snapInfos.Count > 0 ? snapInfos[0].snappedPosition : Vector3.zero;
        }

        public SnapInfo GetSnapInfoForRoad(RoadComponent targetRoad)
        {
            foreach (var info in snapInfos)
            {
                if (info.snappedRoad == targetRoad)
                {
                    return info;
                }
            }
            return snapInfos.Count > 0 ? snapInfos[0] : default(SnapInfo);
        }

        public List<Vector3> GetAllPositions()
        {
            List<Vector3> positions = new List<Vector3>();
            foreach (var info in snapInfos)
            {
                positions.Add(info.snappedPosition);
            }
            return positions;
        }

        public List<RoadComponent> GetAllRoads()
        {
            List<RoadComponent> roads = new List<RoadComponent>();
            foreach (var info in snapInfos)
            {
                roads.Add(info.snappedRoad);
            }
            return roads;
        }

        // 호환성을 위한 프로퍼티
        public List<Vector3> snappedPositions => GetAllPositions();
        public List<RoadComponent> snappedRoads => GetAllRoads();
    }
    #endregion

    public class PlotCreator : MonoBehaviour
    {
        #region Inspector Fields
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Material fillMaterial;

        [Header("Mesh Snap Settings")]
        [SerializeField] private LayerMask snapLayerMask = -1;
        [SerializeField] private float snapDistance = 1.5f;
        #endregion

        #region Public Properties
        [Min(3)]
        public int MaxCountOfVertex { get; private set; } = 4;
        public List<PlotVertex> PlotVertices { get; private set; }
        public Mesh PlotMesh { get; private set; }

        [SerializeField] private bool enableAutoCurveExploration = true;

        // 호환성 및 편의성을 위한 프로퍼티들
        public List<Vector3> VertexPositions => GetVertexPositions();
        public List<bool> IsVertexSnapped => PlotVertices?.Select(v => v.isSnapped).ToList() ?? new List<bool>();
        public List<bool> VertexSnapStates => PlotVertices?.Select(v => v.isSnapped).ToList() ?? new List<bool>();
        public Color LineColor { get; set; } = Color.white;
        public Color FillColor { get; set; } = new Color(1f, 1f, 1f, 0.3f);

        #endregion

        #region Debug Info (Inspector)
        [Header("Debug - Plot Vertices")]
        [SerializeField] private List<PlotVertex> debugPlotVertices = new List<PlotVertex>(); // Inspector에서 확인 가능
        [SerializeField] private List<Vector3> debugFilteredVertices = new List<Vector3>(); // Inspector에서 확인 가능
        #endregion

        #region Private Fields
        // Renderer components
        private LineRenderer[] lineRenderers; // 각 변(edge)에 대한 별도 LineRenderer
        private LineRenderer debugCollisionCheckLR; // 충돌체크 위치 디버그용 LineRenderer
        private LineRenderer[] debugTestLineLRs; // 테스트 선들용 LineRenderer 배열 (4개)
        private LineRenderer debugPerpendicularLR; // PlotCenter에서 직선까지의 수직선용 LineRenderer
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;

        // Internal curve exploration state
        private Dictionary<int, List<Vector3>> edgeCurvePaths = new Dictionary<int, List<Vector3>>(); // 각 변의 곡선 경로
        #endregion        

        #region Unity Lifecycle
        private void OnDestroy()
        {
            ClearAllLineRenderers();
        }
        #endregion

        #region Init
        public void InitializePlot()
        {
            PlotVertices = new List<PlotVertex>();

            InitializeMaterials();
            InitializeRenderers();
            UpdatePlotVisualization();
        }

        private void InitializeRenderers()
        {
            InitializeLineRenderer();
            InitializeMeshRenderer();
        }

        private void InitializeLineRenderer()
        {
            // 기존 LineRenderer들 정리
            ClearAllLineRenderers();

            // MaxCountOfVertex 개수만큼 LineRenderer 생성 (각 변에 하나씩)
            lineRenderers = new LineRenderer[MaxCountOfVertex];

            for (int i = 0; i < MaxCountOfVertex; i++)
            {
                GameObject edgeObj = new GameObject($"Edge_{i}");
                edgeObj.transform.SetParent(transform);

                LineRenderer lr = edgeObj.AddComponent<LineRenderer>();
                lr.material = lineMaterial;
                lr.startWidth = 0.4f;
                lr.endWidth = 0.4f;
                lr.useWorldSpace = true;
                lr.positionCount = 0;

                lineRenderers[i] = lr;
            }

            // 충돌체크 디버그용 LineRenderer 생성
            InitializeDebugCollisionCheckLR();
        }

        private void InitializeDebugCollisionCheckLR()
        {
            // 자유 경로 연결선용 LineRenderer
            if (debugCollisionCheckLR == null)
            {
                GameObject debugObj = new GameObject("Debug_CollisionCheck");
                debugObj.transform.SetParent(transform);

                debugCollisionCheckLR = debugObj.AddComponent<LineRenderer>();
                debugCollisionCheckLR.material = lineMaterial;
                debugCollisionCheckLR.startWidth = 0.1f;
                debugCollisionCheckLR.endWidth = 0.1f;
                debugCollisionCheckLR.useWorldSpace = true;
                debugCollisionCheckLR.positionCount = 0;
                debugCollisionCheckLR.material.color = Color.cyan; // 청록색으로 구분
            }

            // 테스트 선들용 LineRenderer 배열 (4개)
            if (debugTestLineLRs == null)
            {
                debugTestLineLRs = new LineRenderer[4];
                string[] testNames = { "A_Right", "A_Left", "B_Right", "B_Left" };

                for (int i = 0; i < 4; i++)
                {
                    GameObject testObj = new GameObject($"Debug_Test_{testNames[i]}");
                    testObj.transform.SetParent(transform);

                    LineRenderer testLR = testObj.AddComponent<LineRenderer>();
                    testLR.material = lineMaterial;
                    testLR.startWidth = 0.08f;
                    testLR.endWidth = 0.08f;
                    testLR.useWorldSpace = true;
                    testLR.positionCount = 0;

                    debugTestLineLRs[i] = testLR;
                }
            }

            // PlotCenter 수직선용 LineRenderer
            if (debugPerpendicularLR == null)
            {
                GameObject perpObj = new GameObject("Debug_Perpendicular");
                perpObj.transform.SetParent(transform);

                debugPerpendicularLR = perpObj.AddComponent<LineRenderer>();
                debugPerpendicularLR.material = lineMaterial;
                debugPerpendicularLR.startWidth = 0.12f;
                debugPerpendicularLR.endWidth = 0.12f;
                debugPerpendicularLR.useWorldSpace = true;
                debugPerpendicularLR.positionCount = 0;
                debugPerpendicularLR.material.color = Color.magenta; // 마젠타색으로 구분
            }
        }

        private void InitializeMeshRenderer()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            // MeshRenderer 초기 설정
            meshRenderer.material = fillMaterial;
        }
        #endregion

        private void ClearAllLineRenderers()
        {
            if (lineRenderers != null)
            {
                for (int i = 0; i < lineRenderers.Length; i++)
                {
                    if (lineRenderers[i] != null)
                    {
                        if (Application.isPlaying)
                            Destroy(lineRenderers[i].gameObject);
                        else
                            DestroyImmediate(lineRenderers[i].gameObject);
                    }
                }
                lineRenderers = null;
            }

            // 디버그 충돌체크 LineRenderer도 정리
            if (debugCollisionCheckLR != null)
            {
                if (Application.isPlaying)
                    Destroy(debugCollisionCheckLR.gameObject);
                else
                    DestroyImmediate(debugCollisionCheckLR.gameObject);
                debugCollisionCheckLR = null;
            }

            // 디버그 테스트 LineRenderer 배열도 정리
            if (debugTestLineLRs != null)
            {
                for (int i = 0; i < debugTestLineLRs.Length; i++)
                {
                    if (debugTestLineLRs[i] != null)
                    {
                        if (Application.isPlaying)
                            Destroy(debugTestLineLRs[i].gameObject);
                        else
                            DestroyImmediate(debugTestLineLRs[i].gameObject);
                    }
                }
                debugTestLineLRs = null;
            }

            // 디버그 수직선 LineRenderer도 정리
            if (debugPerpendicularLR != null)
            {
                if (Application.isPlaying)
                    Destroy(debugPerpendicularLR.gameObject);
                else
                    DestroyImmediate(debugPerpendicularLR.gameObject);
                debugPerpendicularLR = null;
            }

            // edgeCurvePaths도 함께 정리
            edgeCurvePaths.Clear();
        }

        #region Vertex Management
        public void AddVertex(Vector3 position)
        {
            if (PlotVertices == null) return;
            if (PlotVertices.Count >= MaxCountOfVertex) return;

            PlotVertices.Add(SnapToRoadVertex(position));

            // Debug: PlotVertex 정보 업데이트 (Inspector용)
            UpdateDebugInfo();

            UpdatePlotVisualization();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="index">-1이면 마지막 인덱스</param>
        public void UpdateVertexPosition(Vector3 position, int index = -1)
        {
            if (PlotVertices == null || PlotVertices.Count == 0) return;

            if (index == -1) index = PlotVertices.Count - 1;

            if (index < 0 || index >= PlotVertices.Count) return;

            PlotVertex plotVertex;

            plotVertex = SnapToRoadVertex(position);
            if (enableAutoCurveExploration && PlotVertices.Count >= 2 && PlotVertices != null)
            {
                // 마지막 두 vertex 확인
                int lastIndex = PlotVertices.Count - 1;
                int secondLastIndex = lastIndex - 1;

                // 연속된 두 점이 모두 snap된 경우에만 exploration 실행
                if (PlotVertices[secondLastIndex].isSnapped && PlotVertices[lastIndex].isSnapped)
                {

                    List<Vector3> edgeCurvePath = ExploreCurveVertices(secondLastIndex, lastIndex);

                    if (edgeCurvePath.Count > 0)
                    {
                        // 해당 변(edge)의 곡선 경로 저장
                        edgeCurvePaths[secondLastIndex] = edgeCurvePath;
                    }
                    else
                    {
                        // 곡선 경로를 찾지 못하면 해당 변에서 제거
                        edgeCurvePaths.Remove(secondLastIndex);
                    }
                }
                else
                {
                    // snap되지 않은 경우 해당 변의 곡선 경로 제거
                    edgeCurvePaths.Remove(secondLastIndex);
                }

                // 미리보기 중: 마지막 점과 첫 번째 점 사이의 곡선도 확인 (폐곡선 완성을 위해)
                if (PlotVertices.Count >= 4 && PlotVertices[0].isSnapped && PlotVertices[lastIndex].isSnapped)
                {

                    List<Vector3> edgeCurvePath = ExploreCurveVertices(lastIndex, 0);

                    if (edgeCurvePath.Count > 0)
                    {
                        // 해당 변(edge)의 곡선 경로 저장 (마지막 변 인덱스 사용)
                        edgeCurvePaths[lastIndex] = edgeCurvePath;
                    }
                    else
                    {
                        // 곡선 경로를 찾지 못하면 해당 변에서 제거
                        edgeCurvePaths.Remove(lastIndex);
                    }
                }
                else
                {
                    // snap되지 않은 경우 closing edge의 곡선 경로 제거
                    edgeCurvePaths.Remove(lastIndex);
                }
            }

            PlotVertices[index] = plotVertex;

            // Debug: PlotVertex 정보 업데이트 (Inspector용)
            UpdateDebugInfo();

            UpdatePlotVisualization();
        }

        public void RemoveLastVertex()
        {
            if (PlotVertices == null || PlotVertices.Count == 0) return;

            PlotVertices.RemoveAt(PlotVertices.Count - 1);

            // Debug: PlotVertex 정보 업데이트 (Inspector용)
            UpdateDebugInfo();

            UpdatePlotVisualization();
        }

        public void ClearPlot()
        {
            if (PlotVertices != null)
            {
                PlotVertices.Clear();
            }

            // 곡선 경로 정보도 정리
            edgeCurvePaths.Clear();

            // 디버그 충돌체크 LineRenderer도 비활성화
            if (debugCollisionCheckLR != null)
            {
                debugCollisionCheckLR.positionCount = 0;
            }

            // 디버그 테스트 LineRenderer 배열도 비활성화
            if (debugTestLineLRs != null)
            {
                for (int i = 0; i < debugTestLineLRs.Length; i++)
                {
                    if (debugTestLineLRs[i] != null)
                    {
                        debugTestLineLRs[i].positionCount = 0;
                    }
                }
            }

            // 디버그 수직선 LineRenderer도 비활성화
            if (debugPerpendicularLR != null)
            {
                debugPerpendicularLR.positionCount = 0;
            }

            // Debug: PlotVertex 정보 업데이트 (Inspector용)
            UpdateDebugInfo();

            UpdatePlotVisualization();
        }

        /// <summary>
        /// PlotVertices에서 위치값만 추출하여 반환
        /// </summary>
        public List<Vector3> GetVertexPositions()
        {
            if (PlotVertices == null || PlotVertices.Count == 0)
                return new List<Vector3>();

            List<Vector3> positions = new List<Vector3>(PlotVertices.Count);
            for (int i = 0; i < PlotVertices.Count; i++)
            {
                positions.Add(PlotVertices[i].snappedPositions[0]);
            }
            return positions;
        }

        /// <summary>
        /// 특정 인덱스의 vertex 위치 반환
        /// </summary>
        public Vector3 GetVertexPosition(int index)
        {
            if (PlotVertices == null || index < 0 || index >= PlotVertices.Count)
                return Vector3.zero;

            return PlotVertices[index].snappedPositions[0];
        }

        /// <summary>
        /// 특정 인덱스의 vertex가 snap되었는지 확인
        /// </summary>
        public bool IsVertexSnappedAt(int index)
        {
            if (PlotVertices == null || index < 0 || index >= PlotVertices.Count)
                return false;

            return PlotVertices[index].isSnapped;
        }

        /// <summary>
        /// Inspector 디버깅을 위한 PlotVertex 정보 업데이트
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (PlotVertices != null)
            {
                debugPlotVertices = new List<PlotVertex>(PlotVertices);
            }
            else
            {
                debugPlotVertices.Clear();
            }
        }

        #endregion

        #region Snapping Methods

        /// <summary>
        /// position 위치에서 가장 가까운 Road 점 찾기. 코너 우선.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="isCornerFirst"></param>
        /// <returns></returns>
        private PlotVertex SnapToRoadVertex(Vector3 position, bool isCornerFirst = true)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(position, snapDistance, snapLayerMask);

            bool wasSnapped = false;
            List<SnapInfo> snapInfos = new List<SnapInfo>();

            // 먼저 모든 근처 도로들을 수집
            List<RoadComponent> processedRoads = new List<RoadComponent>();

            foreach (var collider in nearbyColliders)
            {
                GameObject obj = collider.gameObject;
                RoadComponent road = obj.transform.parent?.GetComponent<RoadComponent>();
                if (road != null && !processedRoads.Contains(road))
                {
                    processedRoads.Add(road);
                }
            }

            // 각 도로에서 가장 가까운 점과 라인 타입을 찾음
            foreach (var road in processedRoads)
            {
                Vector3 closestRight = FindClosetPosition(position, road.RightEdgeLine.ToArray());
                Vector3 closestLeft = FindClosetPosition(position, road.LeftEdgeLine.ToArray());

                // 각 라인별로 거리 계산
                float distRight = Vector3.Distance(position, closestRight);
                float distLeft = Vector3.Distance(position, closestLeft);

                // 가장 가까운 라인과 포지션 찾기(right, left 순서)
                Vector3 bestPosition = position;
                SnapLineType bestLineType = SnapLineType.LeftEdge;
                float bestDist = float.MaxValue;

                if (distRight <= snapDistance && distRight <= bestDist)
                {
                    bestDist = distRight;
                    bestPosition = closestRight;
                    bestLineType = SnapLineType.RightEdge;
                }

                if (distLeft <= snapDistance && distLeft <= bestDist)
                {
                    bestDist = distLeft;
                    bestPosition = closestLeft;
                    bestLineType = SnapLineType.LeftEdge;
                }

                // 유효한 스냅이 있으면 추가
                if (bestDist <= snapDistance)
                {
                    snapInfos.Add(new SnapInfo(bestPosition, road, bestLineType));
                    wasSnapped = true;
                }
            }

            // 스냅되지 않은 경우 원래 위치 추가
            if (!wasSnapped)
            {
                snapInfos.Add(new SnapInfo(position, null, SnapLineType.LeftEdge));
            }

            return new PlotVertex(wasSnapped, snapInfos);
        }

        Vector3 FindClosetPosition(Vector3 pos, Vector3[] targetPositions)
        {
            float closestDist = float.MaxValue;
            Vector3 closestPos = targetPositions[0];
            foreach (Vector3 targetPos in targetPositions)
            {
                float dist = Vector3.Distance(pos, targetPos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPos = targetPos;
                }
            }

            return closestPos;
        }
        #endregion
        private void InitializeMaterials()
        {
            if (lineMaterial == null)
            {
                lineMaterial = new Material(Shader.Find("Sprites/Default"))
                {
                    color = LineColor
                };
            }

            if (fillMaterial == null)
            {
                fillMaterial = new Material(Shader.Find("Sprites/Default"))
                {
                    color = FillColor
                };
            }
        }

        #region Visualization
        private void UpdatePlotVisualization()
        {
            if (PlotVertices == null) return;

            int vertexCount = PlotVertices.Count;

            // 라인 렌더링: 2개 이상의 vertex가 있을 때
            if (vertexCount >= 2)
            {
                UpdateLineMesh();
            }
            else
            {
                ClearLineMesh();
            }

            // 폴리곤 채우기: 4개 이상의 vertex가 있을 때만
            if (vertexCount >= 4)
            {
                UpdateFillMesh();
            }
            else
            {
                ClearFillMesh();
            }
        }

        private void UpdateLineMesh()
        {
            if (PlotVertices == null || PlotVertices.Count < 2 || lineRenderers == null) return;

            // 모든 LineRenderer 초기화
            for (int i = 0; i < lineRenderers.Length; i++)
            {
                if (lineRenderers[i] != null)
                {
                    lineRenderers[i].positionCount = 0;
                    lineRenderers[i].material.color = LineColor;
                }
            }

            // 각 변(edge)에 대해 LineRenderer 설정
            for (int i = 0; i < PlotVertices.Count; i++)
            {
                if (i >= lineRenderers.Length) break;

                int nextIndex = (i + 1) % PlotVertices.Count;

                // 마지막 변은 3개 이상의 점이 있을 때만
                if (i == PlotVertices.Count - 1 && PlotVertices.Count < 3) break;

                LineRenderer edgeLR = lineRenderers[i];
                if (edgeLR == null) continue;

                Vector3 startVertex = PlotVertices[i].snappedPositions[0];
                Vector3 endVertex = PlotVertices[nextIndex].snappedPositions[0];

                // 해당 변에 곡선 경로가 있는지 확인
                if (edgeCurvePaths.ContainsKey(i) && edgeCurvePaths[i].Count > 1)
                {
                    // 곡선 경로 사용
                    List<Vector3> smoothedCurve = GenerateSmoothCurve(edgeCurvePaths[i]);

                    edgeLR.positionCount = smoothedCurve.Count;
                    for (int j = 0; j < smoothedCurve.Count; j++)
                    {
                        edgeLR.SetPosition(j, smoothedCurve[j]);
                    }
                }
                else
                {
                    // 직선 사용
                    edgeLR.positionCount = 2;
                    edgeLR.SetPosition(0, startVertex);
                    edgeLR.SetPosition(1, endVertex);
                }
            }
        }

        private void ClearLineMesh()
        {
            if (lineRenderers != null)
            {
                for (int i = 0; i < lineRenderers.Length; i++)
                {
                    if (lineRenderers[i] != null)
                    {
                        lineRenderers[i].positionCount = 0;
                    }
                }
            }
        }

        private void UpdateFillMesh()
        {
            if (PlotVertices == null || PlotVertices.Count < 3 || meshFilter == null || meshRenderer == null) return;

            // 색상 업데이트
            meshRenderer.material.color = FillColor;

            // 메시 생성
            Mesh mesh = new Mesh();

            // 폴리곤 triangulation (간단한 fan triangulation)
            Vector3[] vertices = new Vector3[PlotVertices.Count];
            Vector2[] uvs = new Vector2[PlotVertices.Count];
            int[] triangles = new int[(PlotVertices.Count - 2) * 3];

            // 정점과 UV 설정
            for (int i = 0; i < PlotVertices.Count; i++)
            {
                vertices[i] = PlotVertices[i].snappedPositions[0];
                uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
            }

            // Fan triangulation으로 삼각형 생성
            int triangleIndex = 0;
            for (int i = 1; i < PlotVertices.Count - 1; i++)
            {
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = i;
                triangles[triangleIndex + 2] = i + 1;
                triangleIndex += 3;
            }

            // 메시 설정
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
            PlotMesh = mesh;
        }

        private void ClearFillMesh()
        {
            if (meshFilter != null && meshFilter.mesh != null)
            {
                meshFilter.mesh.Clear();
            }
        }
        #endregion        

        #region Curve Vertex Exploration
        /// <summary>
        /// 주어진 점들을 부드러운 곡선으로 변환
        /// </summary>
        private List<Vector3> GenerateSmoothCurve(List<Vector3> controlPoints)
        {
            if (controlPoints.Count < 2) return controlPoints;

            List<Vector3> smoothedPoints = new List<Vector3>();
            int segmentsPerCurve = 4; // 각 구간당 세분화 수

            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                Vector3 p0 = i > 0 ? controlPoints[i - 1] : controlPoints[i];
                Vector3 p1 = controlPoints[i];
                Vector3 p2 = controlPoints[i + 1];
                Vector3 p3 = i < controlPoints.Count - 2 ? controlPoints[i + 2] : controlPoints[i + 1];

                // Catmull-Rom 스플라인으로 부드러운 곡선 생성
                for (int j = 0; j < segmentsPerCurve; j++)
                {
                    float t = j / (float)segmentsPerCurve;
                    Vector3 point = CalculateCatmullRomPoint(p0, p1, p2, p3, t);
                    smoothedPoints.Add(point);
                }
            }

            // 마지막 점 추가
            smoothedPoints.Add(controlPoints[controlPoints.Count - 1]);

            return smoothedPoints;
        }

        /// <summary>
        /// Catmull-Rom 스플라인 계산
        /// </summary>
        private Vector3 CalculateCatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        /// <summary>
        /// 인접한 두 점이 모두 snap된 상태일 때 두 점 사이의 연결된 도로들을 탐색하여 curve path를 생성
        /// </summary>
        public List<Vector3> ExploreCurveVertices(int startVertexIndex, int endVertexIndex)
        {
            // 두 점이 모두 snap된 상태인지 확인
            if (!PlotVertices[startVertexIndex].isSnapped || !PlotVertices[endVertexIndex].isSnapped)
            {
                Debug.Log("Both vertices must be snapped to generate curve");
                return new List<Vector3>();
            }

            // 시작점과 끝점 정보 가져오기
            PlotVertex startVertex = PlotVertices[startVertexIndex];
            PlotVertex endVertex = PlotVertices[endVertexIndex];

            if (startVertex.snappedRoads.Count == 0 || endVertex.snappedRoads.Count == 0)
            {
                Debug.Log("No snapped roads found for curve generation");
                return new List<Vector3>();
            }

            // 공통 도로가 있는지 확인 (같은 도로에서 시작과 끝이 모두 snap된 경우)
            RoadComponent commonRoad = FindCommonRoad(startVertex.snappedRoads, endVertex.snappedRoads);

            if (commonRoad != null)
            {
                // 같은 도로 내에서 곡선 생성
                return GenerateCurveWithinSameRoad(startVertex, endVertex, commonRoad);
            }
            else
            {
                // 서로 다른 도로 간 곡선 생성
                return GenerateCurveBetweenDifferentRoads(startVertex, endVertex);
            }
        }

        /// <summary>
        /// 두 도로 리스트에서 공통 도로를 찾습니다
        /// </summary>
        private RoadComponent FindCommonRoad(List<RoadComponent> roadsA, List<RoadComponent> roadsB)
        {
            foreach (var roadA in roadsA)
            {
                foreach (var roadB in roadsB)
                {
                    if (roadA == roadB)
                        return roadA;
                }
            }
            return null;
        }

        /// <summary>
        /// 같은 도로 내에서 곡선을 생성합니다
        /// </summary>
        private List<Vector3> GenerateCurveWithinSameRoad(PlotVertex startVertex, PlotVertex endVertex, RoadComponent road)
        {
            SnapInfo startInfo = startVertex.GetSnapInfoForRoad(road);
            SnapInfo endInfo = endVertex.GetSnapInfoForRoad(road);

            Vector3 startPos = startInfo.snappedPosition;
            Vector3 endPos = endInfo.snappedPosition;

            // 같은 라인 타입이면 해당 라인을 따라 곡선 생성
            if (startInfo.snappedLine == endInfo.snappedLine)
            {
                List<Vector3> targetLine = GetEdgeLineByType(road, startInfo.snappedLine);
                List<Vector3> curvePoints = ExtractPointsBetweenOnLine(targetLine, startPos, endPos);

                if (curvePoints.Count > 2)
                {
                    Debug.Log($"Generated curve on {startInfo.snappedLine} line with {curvePoints.Count} points");
                    return curvePoints;
                }
            }
            else
            {
                // 서로 다른 라인 타입이면 두 라인을 모두 활용한 곡선 생성
                List<Vector3> startLine = GetEdgeLineByType(road, startInfo.snappedLine);
                List<Vector3> endLine = GetEdgeLineByType(road, endInfo.snappedLine);

                List<Vector3> curvePoints = CreateCurveAcrossLines(startLine, endLine, startPos, endPos);

                if (curvePoints.Count > 2)
                {
                    Debug.Log($"Generated curve across {startInfo.snappedLine} to {endInfo.snappedLine} with {curvePoints.Count} points");
                    return curvePoints;
                }
            }

            return new List<Vector3>(); // 직선으로 처리
        }

        /// <summary>
        /// 라인 타입에 따라 해당 엣지 라인을 반환합니다
        /// </summary>
        private List<Vector3> GetEdgeLineByType(RoadComponent road, SnapLineType lineType)
        {
            switch (lineType)
            {
                case SnapLineType.LeftEdge:
                    return road.LeftEdgeLine;
                case SnapLineType.RightEdge:
                    return road.RightEdgeLine;
                default:
                    return road.LeftEdgeLine;
            }
        }

        /// <summary>
        /// 같은 라인에서 두 점 사이의 점들을 추출합니다
        /// </summary>
        private List<Vector3> ExtractPointsBetweenOnLine(List<Vector3> line, Vector3 startPos, Vector3 endPos)
        {
            List<Vector3> points = new List<Vector3>();
            const float tolerance = 0.2f;

            if (line == null || line.Count < 2)
                return points;

            // 1단계: 시작점과 끝점의 인덱스 찾기
            int startIndex = -1;
            int endIndex = -1;
            float closestStartDist = float.MaxValue;
            float closestEndDist = float.MaxValue;

            for (int i = 0; i < line.Count; i++)
            {
                Vector3 linePoint = line[i];

                // 시작점에 가장 가까운 인덱스 찾기
                float distToStart = Vector3.Distance(linePoint, startPos);
                if (distToStart <= tolerance && distToStart < closestStartDist)
                {
                    closestStartDist = distToStart;
                    startIndex = i;
                }

                // 끝점에 가장 가까운 인덱스 찾기
                float distToEnd = Vector3.Distance(linePoint, endPos);
                if (distToEnd <= tolerance && distToEnd < closestEndDist)
                {
                    closestEndDist = distToEnd;
                    endIndex = i;
                }
            }

            // 유효한 인덱스를 찾지 못한 경우
            if (startIndex == -1 || endIndex == -1)
            {
                Debug.Log($"Could not find valid indices on line. startIndex: {startIndex}, endIndex: {endIndex}");
                return points;
            }

            // 2단계: 인덱스 범위 결정 (항상 작은 인덱스부터 큰 인덱스까지)
            int minIndex = Mathf.Min(startIndex, endIndex);
            int maxIndex = Mathf.Max(startIndex, endIndex);
            bool needReverse = startIndex > endIndex;

            // 시작점 추가
            points.Add(startPos);

            // minIndex+1부터 maxIndex-1까지 중간 점들 수집
            for (int i = minIndex + 1; i < maxIndex; i++)
            {
                Vector3 linePoint = line[i];

                // 시작점/끝점과 너무 가까운 중간점은 제외
                if (Vector3.Distance(linePoint, startPos) > tolerance &&
                    Vector3.Distance(linePoint, endPos) > tolerance)
                {
                    points.Add(linePoint);
                }
            }

            // 끝점 추가
            points.Add(endPos);

            // 필요하면 중간 점들만 뒤집기 (시작점과 끝점은 그대로 유지)
            if (needReverse && points.Count > 2)
            {
                // 시작점(0)과 끝점(마지막)을 제외한 중간 부분만 뒤집기
                List<Vector3> middlePoints = points.GetRange(1, points.Count - 2);
                middlePoints.Reverse();

                // 다시 조합: 시작점 + 뒤집힌 중간점들 + 끝점
                List<Vector3> reversedPoints = new List<Vector3> { points[0] };
                reversedPoints.AddRange(middlePoints);
                reversedPoints.Add(points[points.Count - 1]);

                points = reversedPoints;
            }

            Debug.Log($"Extracted {points.Count} points from line. Direction: {(needReverse ? "Reverse" : "Forward")}, StartIdx: {startIndex}, EndIdx: {endIndex}");
            return points;
        }

        /// <summary>
        /// 서로 다른 라인들 사이의 곡선을 생성합니다
        /// </summary>
        private List<Vector3> CreateCurveAcrossLines(List<Vector3> startLine, List<Vector3> endLine, Vector3 startPos, Vector3 endPos)
        {
            List<Vector3> points = new List<Vector3> { startPos };

            // 간단한 구현: 중점을 이용한 부드러운 곡선
            Vector3 midPoint = (startPos + endPos) * 0.5f;

            // 시작점과 끝점의 라인 방향을 고려한 중점 오프셋
            Vector3 startDirection = GetDirectionAtPoint(startLine, startPos);
            Vector3 endDirection = GetDirectionAtPoint(endLine, endPos);

            Vector3 offsetMidPoint = midPoint + (startDirection + endDirection) * 0.1f;
            points.Add(offsetMidPoint);
            points.Add(endPos);

            return points;
        }

        /// <summary>
        /// 라인에서 특정 점의 방향을 계산합니다
        /// </summary>
        private Vector3 GetDirectionAtPoint(List<Vector3> line, Vector3 point)
        {
            if (line == null || line.Count < 2)
                return Vector3.forward;

            // 가장 가까운 라인 세그먼트의 방향 반환
            float closestDistance = float.MaxValue;
            Vector3 direction = Vector3.forward;

            for (int i = 0; i < line.Count - 1; i++)
            {
                Vector3 segmentStart = line[i];
                Vector3 segmentEnd = line[i + 1];

                float distToSegment = GetDistancePointToSegment(point, segmentStart, segmentEnd);
                if (distToSegment < closestDistance)
                {
                    closestDistance = distToSegment;
                    direction = (segmentEnd - segmentStart).normalized;
                }
            }

            return direction;
        }

        /// <summary>
        /// 점과 선분 사이의 최단 거리를 계산합니다
        /// </summary>
        private float GetDistancePointToSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
        {
            Vector3 segmentVector = segmentEnd - segmentStart;
            Vector3 pointVector = point - segmentStart;

            float segmentLength = segmentVector.magnitude;
            if (segmentLength < 0.001f)
                return Vector3.Distance(point, segmentStart);

            float t = Mathf.Clamp01(Vector3.Dot(pointVector, segmentVector) / (segmentLength * segmentLength));
            Vector3 closestPointOnSegment = segmentStart + t * segmentVector;

            return Vector3.Distance(point, closestPointOnSegment);
        }

        /// <summary>
        /// 서로 다른 도로 간 곡선을 생성합니다
        /// </summary>
        private List<Vector3> GenerateCurveBetweenDifferentRoads(PlotVertex startVertex, PlotVertex endVertex)
        {
            List<Vector3> totalPath = new List<Vector3>();

            // 시작점과 끝점의 첫 번째 스냅 정보 가져오기
            SnapInfo startInfo = startVertex.snapInfos[0];
            SnapInfo endInfo = endVertex.snapInfos[0];

            Vector3 currentPos = startInfo.snappedPosition;
            Vector3 finalEndPos = endInfo.snappedPosition;
            RoadComponent currentRoad = startInfo.snappedRoad;
            RoadComponent targetRoad = endInfo.snappedRoad;

            // 1. 시작점 포함한 엣지에서 도착점에 가까운 엣지 끝까지 경로 저장
            List<Vector3> startEdgeLine = GetEdgeLineByType(currentRoad, startInfo.snappedLine);
            Vector3 closestEndOfStartEdge = GetClosestEdgeEndToTarget(startEdgeLine, finalEndPos);

            // 시작 엣지의 부분 경로 추가
            List<Vector3> startEdgePath = ExtractPointsBetweenOnLine(startEdgeLine, currentPos, closestEndOfStartEdge);
            if (startEdgePath.Count > 1)
            {
                totalPath.AddRange(startEdgePath.GetRange(0, startEdgePath.Count - 1)); // 끝점 제외
            }

            currentPos = closestEndOfStartEdge;
            const int maxIterations = 10;
            int iterations = 0;

            // 2-5. 중간 도로들을 통한 경로 탐색
            while (iterations < maxIterations)
            {
                iterations++;

                // 2. physics.sphere로 nextRoad 탐색
                RoadComponent nextRoad = FindNextRoadFromPosition(currentPos, currentRoad);
                if (nextRoad == null)
                {
                    Debug.Log("No next road found, connecting directly to end");
                    break;
                }

                // 3-1. nextRoad가 도착점 road면 종료
                if (nextRoad == targetRoad)
                {
                    Debug.Log("Reached target road");
                    break;
                }

                // 3-2. nextRoad의 양 끝점(총 6개) 중 끝점에 가까운 점을 포함한 엣지 선택
                SnapLineType bestLineType;
                Vector3 bestStartPoint, bestEndPoint;
                FindBestEdgeOnRoad(nextRoad, currentPos, finalEndPos, out bestLineType, out bestStartPoint, out bestEndPoint);

                // 4. 해당 엣지의 시작점과 끝점으로 경로 저장
                List<Vector3> nextEdgeLine = GetEdgeLineByType(nextRoad, bestLineType);
                List<Vector3> nextEdgePath = ExtractPointsBetweenOnLine(nextEdgeLine, bestStartPoint, bestEndPoint);

                if (nextEdgePath.Count > 1)
                {
                    // 첫 번째 점 제외하고 추가 (연결점 중복 방지)
                    totalPath.AddRange(nextEdgePath.GetRange(1, nextEdgePath.Count - 1));
                }

                currentPos = bestEndPoint;
                currentRoad = nextRoad;
            }

            // 6. 도착점 포함한 엣지에서 시작점에 가까운 엣지 끝까지 경로 저장
            List<Vector3> endEdgeLine = GetEdgeLineByType(targetRoad, endInfo.snappedLine);
            Vector3 closestStartOfEndEdge = GetClosestEdgeEndToTarget(endEdgeLine, currentPos);

            List<Vector3> endEdgePath = ExtractPointsBetweenOnLine(endEdgeLine, closestStartOfEndEdge, finalEndPos);
            if (endEdgePath.Count > 1)
            {
                // 첫 번째 점 제외하고 추가 (연결점 중복 방지)
                totalPath.AddRange(endEdgePath.GetRange(1, endEdgePath.Count - 1));
            }
            else
            {
                totalPath.Add(finalEndPos);
            }

            Debug.Log($"Generated cross-road curve with {totalPath.Count} points across {iterations} roads");
            return totalPath.Count > 2 ? totalPath : new List<Vector3>();
        }

        /// <summary>
        /// 특정 위치에서 다음 도로를 찾습니다
        /// </summary>
        private RoadComponent FindNextRoadFromPosition(Vector3 position, RoadComponent currentRoad)
        {
            const float searchRadius = 0.4f;
            Collider[] nearbyColliders = Physics.OverlapSphere(position, searchRadius, snapLayerMask);

            foreach (var collider in nearbyColliders)
            {
                RoadComponent road = collider.transform.parent?.GetComponent<RoadComponent>();
                if (road != null && road != currentRoad)
                {
                    return road;
                }
            }

            return null;
        }

        /// <summary>
        /// 도로에서 가장 적합한 엣지를 찾습니다
        /// </summary>
        private void FindBestEdgeOnRoad(RoadComponent road, Vector3 currentPos, Vector3 targetPos,
            out SnapLineType bestLineType, out Vector3 bestStartPoint, out Vector3 bestEndPoint)
        {
            bestLineType = SnapLineType.LeftEdge;
            bestStartPoint = Vector3.zero;
            bestEndPoint = Vector3.zero;
            float closestDistanceToTarget = float.MaxValue;

            // 모든 엣지 라인의 양 끝점 확인
            SnapLineType[] lineTypes = { SnapLineType.LeftEdge, SnapLineType.RightEdge };

            foreach (var lineType in lineTypes)
            {
                List<Vector3> edgeLine = GetEdgeLineByType(road, lineType);
                if (edgeLine == null || edgeLine.Count < 2) continue;

                Vector3 edgeStart = edgeLine[0];
                Vector3 edgeEnd = edgeLine[edgeLine.Count - 1];

                // 양 끝점 중 targetPos에 더 가까운 점 선택
                float distStartToTarget = Vector3.Distance(edgeStart, targetPos);
                float distEndToTarget = Vector3.Distance(edgeEnd, targetPos);

                Vector3 closerToTarget = distStartToTarget < distEndToTarget ? edgeStart : edgeEnd;
                float distanceToTarget = Mathf.Min(distStartToTarget, distEndToTarget);

                if (distanceToTarget < closestDistanceToTarget)
                {
                    closestDistanceToTarget = distanceToTarget;
                    bestLineType = lineType;

                    // 방향 결정: currentPos에서 더 먼 점이 시작점
                    float distStartToCurrent = Vector3.Distance(edgeStart, currentPos);
                    float distEndToCurrent = Vector3.Distance(edgeEnd, currentPos);

                    if (distStartToCurrent > distEndToCurrent)
                    {
                        bestStartPoint = edgeStart;
                        bestEndPoint = edgeEnd;
                    }
                    else
                    {
                        bestStartPoint = edgeEnd;
                        bestEndPoint = edgeStart;
                    }
                }
            }
        }

        /// <summary>
        /// 엣지 라인의 양 끝점 중 타겟에 더 가까운 점을 반환합니다
        /// </summary>
        private Vector3 GetClosestEdgeEndToTarget(List<Vector3> edgeLine, Vector3 targetPos)
        {
            if (edgeLine == null || edgeLine.Count < 2)
                return targetPos;

            Vector3 start = edgeLine[0];
            Vector3 end = edgeLine[edgeLine.Count - 1];

            float distToStart = Vector3.Distance(start, targetPos);
            float distToEnd = Vector3.Distance(end, targetPos);

            return distToStart < distToEnd ? start : end;
        }

        /// <summary>
        /// 도로의 엣지 라인에서 두 점 사이의 점들을 추출합니다
        /// </summary>
        private List<Vector3> ExtractEdgePointsBetween(RoadComponent road, Vector3 startPos, Vector3 endPos)
        {
            List<Vector3> points = new List<Vector3>();
            const float tolerance = 0.2f;

            // 시작점에 더 가까운 엣지 라인 선택
            List<Vector3> targetEdge = GetClosestEdgeToPoint(road, startPos);

            if (targetEdge == null || targetEdge.Count < 2)
                return points;

            // 시작점과 끝점 사이의 엣지 포인트들 수집
            bool collecting = false;
            for (int i = 0; i < targetEdge.Count; i++)
            {
                Vector3 edgePoint = targetEdge[i];

                // 시작점과 가까운 지점에서 수집 시작
                if (!collecting && Vector3.Distance(edgePoint, startPos) <= tolerance)
                {
                    collecting = true;
                    points.Add(startPos); // 정확한 시작점 추가
                }

                // 수집 중인 경우 점 추가
                if (collecting)
                {
                    // 시작점/끝점과 너무 가까운 중간점은 제외
                    if (Vector3.Distance(edgePoint, startPos) > tolerance &&
                        Vector3.Distance(edgePoint, endPos) > tolerance)
                    {
                        points.Add(edgePoint);
                    }
                }

                // 끝점과 가까운 지점에서 수집 종료
                if (collecting && Vector3.Distance(edgePoint, endPos) <= tolerance)
                {
                    points.Add(endPos); // 정확한 끝점 추가
                    break;
                }
            }

            return points;
        }

        /// <summary>
        /// 점에 가장 가까운 엣지 라인을 반환합니다
        /// </summary>
        private List<Vector3> GetClosestEdgeToPoint(RoadComponent road, Vector3 point)
        {
            float distToLeft = GetDistanceToEdgeLine(road.LeftEdgeLine, point);
            float distToRight = GetDistanceToEdgeLine(road.RightEdgeLine, point);

            if (distToLeft <= distToRight)
                return road.LeftEdgeLine;
            else
                return road.RightEdgeLine;
        }

        /// <summary>
        /// 점과 엣지 라인 사이의 최단 거리를 계산합니다
        /// </summary>
        private float GetDistanceToEdgeLine(List<Vector3> edgeLine, Vector3 point)
        {
            if (edgeLine == null || edgeLine.Count == 0)
                return float.MaxValue;

            float minDistance = float.MaxValue;
            foreach (var edgePoint in edgeLine)
            {
                float distance = Vector3.Distance(point, edgePoint);
                if (distance < minDistance)
                    minDistance = distance;
            }
            return minDistance;
        }

        /// <summary>
        /// 두 도로 사이의 연결 경로를 찾습니다
        /// </summary>
        private List<Vector3> FindConnectionPath(RoadComponent startRoad, RoadComponent endRoad, Vector3 startPos, Vector3 endPos)
        {
            List<Vector3> path = new List<Vector3> { startPos };

            // 간단한 구현: 직선 연결
            // 향후 복잡한 경로 탐색 알고리즘으로 확장 가능
            Vector3 midPoint = (startPos + endPos) * 0.5f;
            path.Add(midPoint);
            path.Add(endPos);

            return path;
        }

        /// <summary>
        /// 현재 mesh에 인접한(닿아있는) mesh들을 해당 mesh의 모든 vertex에서 작은 OverlapSphere를 사용해 탐색
        /// 각 vertex 위치에서 작은 구체로 인접한 mesh들을 감지
        /// </summary>
        private List<Collider> FindAdjacentMeshes(Collider currentMesh, Vector3 snapPoint)
        {
            List<Collider> adjacentMeshes = new List<Collider>();
            const float vertexOverlapSphereRadius = 0.1f;

            // 현재 mesh의 모든 vertex 위치 가져오기
            Vector3[] worldVertices = GetMeshWorldVertices(currentMesh);

            if (worldVertices == null || worldVertices.Length == 0)
            {
                Debug.LogWarning($"[FindAdjacentMeshes] No vertices found for mesh: {currentMesh.gameObject.name}");
                return adjacentMeshes;
            }

            // 각 vertex에서 작은 OverlapSphere 생성하여 인접 mesh 탐색
            for (int i = 0; i < worldVertices.Length; i++)
            {
                Vector3 vertexPos = worldVertices[i];

                Collider[] foundColliders = Physics.OverlapSphere(
                    vertexPos,
                    vertexOverlapSphereRadius,
                    snapLayerMask
                );

                foreach (var collider in foundColliders)
                {
                    // 현재 mesh가 아니고, 아직 발견되지 않은 mesh라면 추가
                    if (collider != currentMesh && !adjacentMeshes.Contains(collider))
                    {
                        adjacentMeshes.Add(collider);
                    }
                }
            }

            return adjacentMeshes;
        }

        /// <summary>
        /// Mesh의 모든 vertex를 월드 좌표로 변환하여 반환
        /// </summary>
        private Vector3[] GetMeshWorldVertices(Collider meshCollider)
        {
            MeshFilter meshFilter = meshCollider.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.mesh == null)
                return null;

            Mesh mesh = meshFilter.mesh;
            Transform meshTransform = meshCollider.transform;
            Vector3[] vertices = mesh.vertices;

            if (vertices.Length == 0)
                return null;

            // 로컬 좌표를 월드 좌표로 변환
            Vector3[] worldVertices = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                worldVertices[i] = meshTransform.TransformPoint(vertices[i]);
            }

            return worldVertices;
        }

        /// <summary>
        /// Collider에서 RoadComponent를 찾습니다. 없으면 parent hierarchy에서 검색합니다.
        /// </summary>
        private RoadComponent FindRoadComponent(Collider collider)
        {
            if (collider == null) return null;

            // 1. 해당 GameObject에서 먼저 찾기
            RoadComponent roadComponent = collider.GetComponent<RoadComponent>();
            if (roadComponent != null) return roadComponent;

            // 2. Parent hierarchy에서 찾기
            Transform current = collider.transform.parent;
            while (current != null)
            {
                roadComponent = current.GetComponent<RoadComponent>();
                if (roadComponent != null)
                {
                    Debug.Log($"Found RoadComponent on parent: {current.name}");
                    return roadComponent;
                }
                current = current.parent;
            }

            return null;
        }

        /// <summary>
        /// 주어진 점이 RoadComponent의 어느 edgeLine에 더 가까운지 판단합니다.
        /// </summary>
        private List<Vector3> GetClosestEdgeLine(RoadComponent roadComponent, Vector3 worldPoint)
        {
            if (roadComponent == null) return null;

            float distanceToLeft = GetMinDistanceToEdgeLine(roadComponent, roadComponent.LeftEdgeLine, worldPoint);
            float distanceToRight = GetMinDistanceToEdgeLine(roadComponent, roadComponent.RightEdgeLine, worldPoint);

            // 더 가까운 edgeLine 반환
            return distanceToLeft <= distanceToRight ? roadComponent.LeftEdgeLine : roadComponent.RightEdgeLine;
        }

        /// <summary>
        /// 점과 edgeLine 사이의 최단 거리를 계산합니다.
        /// </summary>
        private float GetMinDistanceToEdgeLine(RoadComponent roadComponent, List<Vector3> edgeLine, Vector3 worldPoint)
        {
            if (edgeLine == null || edgeLine.Count == 0) return float.MaxValue;

            float minDistance = float.MaxValue;
            foreach (var localPoint in edgeLine)
            {
                Vector3 worldEdgePoint = roadComponent.transform.TransformPoint(localPoint);
                float distance = Vector3.Distance(worldPoint, worldEdgePoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return minDistance;
        }

        /// <summary>
        /// vertex가 시작점과 도착점 사이에 위치하는지 정확히 판단
        /// </summary>
        private bool IsVertexBetweenStartAndEnd(Vector3 startPos, Vector3 endPos, Vector3 vertex)
        {
            // 시작점-도착점 선분의 방향 벡터
            Vector3 lineDirection = (endPos - startPos);
            float lineLength = lineDirection.magnitude;
            lineDirection = lineDirection.normalized;

            // 시작점에서 vertex로의 벡터
            Vector3 startToVertex = vertex - startPos;

            // vertex를 시작점-도착점 직선에 투영
            float projectionLength = Vector3.Dot(startToVertex, lineDirection);

            // 투영된 길이가 0~lineLength 범위 내에 있으면 "사이"에 위치
            return projectionLength >= 0f && projectionLength <= lineLength;
        }





        #endregion
    }
}