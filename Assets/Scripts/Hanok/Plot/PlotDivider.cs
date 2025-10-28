using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    public class PlotDivider : MonoBehaviour
    {
        #region Inspector Fields
        [SerializeField] private Material lineMaterial;                 // 공유용 1개
        [SerializeField] private Color semiPlotColor = Color.yellow;    // 세미플롯 라인 색
        [SerializeField] private Color markerColor   = Color.yellow;    // 엣지 마커 라인 색
        [SerializeField] private float markerLength  = 0.4f;            // 엣지 마커 길이
        #endregion

        #region Private Fields
        private List<Plot> currentSemiPlots;

        private const string SemiPlotPrefix = "SemiPlot_";
        private const string MarkerPrefix   = "EdgeMarker_";

        private bool _initialized;
        private Transform _semiPlotsParent;
        private Transform _edgeMarkersParent;
        #endregion

        void Awake() => InitOnce();

        private void InitOnce()
        {
            if (_initialized) return;
            _initialized = true;

            if (!lineMaterial)
                lineMaterial = new Material(Shader.Find("Sprites/Default"));

            // 컨테이너(부모) 1회 준비
            _semiPlotsParent   = transform.Find("SemiPlots")   ?? new GameObject("SemiPlots").transform;
            _edgeMarkersParent = transform.Find("EdgeMarkers") ?? new GameObject("EdgeMarkers").transform;
            _semiPlotsParent.SetParent(transform, false);
            _edgeMarkersParent.SetParent(transform, false);
        }

        // 외부에서 parent를 넘기면 컨테이너를 그 아래로 옮겨 배치(선택적)
        private void EnsureParents(Transform parent)
        {
            InitOnce();
            if (parent == null) return;

            if (_semiPlotsParent.parent != parent)
                _semiPlotsParent.SetParent(parent, false);
            if (_edgeMarkersParent.parent != parent)
                _edgeMarkersParent.SetParent(parent, false);
        }

        #region SemiPlot Preview
        public void ShowSemiPlotPreview(Plot plot, int unitCount, Transform parent)
        {
            EnsureParents(parent);

            if (plot?.PlotVertices == null || plot.PlotVertices.Count != 4 || unitCount < 1)
            {
                ClearSemiPlotPreview();
                return;
            }

            currentSemiPlots = DividePlot(plot, unitCount);
            if (currentSemiPlots == null)
            {
                ClearSemiPlotPreview();
                return;
            }

            VisualizeSemiPlots(currentSemiPlots);
        }

        public List<Plot> GetCurrentSemiPlots() => currentSemiPlots;

        public void ClearSemiPlotPreview()
        {
            InitOnce();
            for (int i = 0; i < _semiPlotsParent.childCount; i++)
            {
                var c = _semiPlotsParent.GetChild(i);
                if (c && c.name.StartsWith(SemiPlotPrefix))
                    c.gameObject.SetActive(false);
            }
            currentSemiPlots = null;
        }

        private void VisualizeSemiPlots(List<Plot> semiPlots)
        {
            InitOnce();

            int needed = semiPlots.Count;

            // 1) 필요한 개수만큼 생성/갱신
            for (int i = 0; i < needed; i++)
            {
                var go = GetOrCreateChild(_semiPlotsParent, $"{SemiPlotPrefix}{i}");
                go.SetActive(true);

                // Plot에서 외곽선 정점들 가져오기
                var semiPlot = semiPlots[i];
                var verts = semiPlot.GetFlattenedOutlineVertices();
                if (verts.Count == 0) continue;

                var lr = InitializeSemiPlotLineRenderer(go, verts.Count);
                for (int j = 0; j < verts.Count; j++) lr.SetPosition(j, verts[j]);
            }

            // 2) 남는 것 비활성화
            for (int i = needed; i < _semiPlotsParent.childCount; i++)
            {
                var c = _semiPlotsParent.GetChild(i);
                if (c && c.name.StartsWith(SemiPlotPrefix))
                    c.gameObject.SetActive(false);
            }
        }

        private LineRenderer InitializeSemiPlotLineRenderer(GameObject go, int positionCount)
        {
            var lr = go.GetComponent<LineRenderer>();
            if (!lr)
            {
                lr = go.AddComponent<LineRenderer>();
                lr.material      = lineMaterial;  // 공유 머티리얼
                lr.startWidth    = 0.1f;
                lr.endWidth      = 0.1f;
                lr.useWorldSpace = true;
                lr.loop          = true;          // 닫힌 도형
            }

            lr.startColor = lr.endColor = semiPlotColor;
            if (lr.positionCount != positionCount)
                lr.positionCount = positionCount;

            return lr;
        }
        #endregion

        #region Edge Markers Preview
        public void ShowEdgeMarkersPreview(Plot plot, float minUnitLength, float maxUnitLength, Transform parent)
        {
            EnsureParents(parent);

            if (plot?.PlotVertices == null || plot.PlotVertices.Count < 2)
            {
                Clear();
                return;
            }

            // 첫 번째 변(line 1) 분할 가능 수량 계산
            List<Vector3> firstEdgeVertices = new List<Vector3> { plot.GetVertexPosition(0), plot.GetVertexPosition(1) };
            var (firstUnitCount, firstUnitLen, firstTotalLen) = CalculateOptimalEdgeMarkers(firstEdgeVertices, minUnitLength, maxUnitLength);

            int finalUnitCount = firstUnitCount;

            // 4개 정점이 있는 경우 세 번째 변(line 3)도 확인
            if (plot.PlotVertices.Count >= 4)
            {
                List<Vector3> thirdEdgeVertices = new List<Vector3> { plot.GetVertexPosition(2), plot.GetVertexPosition(3) };
                var (thirdUnitCount, thirdUnitLen, thirdTotalLen) = CalculateOptimalEdgeMarkers(thirdEdgeVertices, minUnitLength, maxUnitLength);

                // 두 변 중 분할 가능 수량이 적은 쪽 선택
                finalUnitCount = Mathf.Min(firstUnitCount, thirdUnitCount);
            }

            if (finalUnitCount < 1)
            {
                Clear();
                return;
            }

            CreateOrUpdateEdgeMarkers(firstEdgeVertices, finalUnitCount);

            if (plot.PlotVertices.Count == 4)
                ShowSemiPlotPreview(plot, finalUnitCount, parent);
        }

        public void ClearEdgeMarkersPreview()
        {
            InitOnce();
            for (int i = 0; i < _edgeMarkersParent.childCount; i++)
            {
                var c = _edgeMarkersParent.GetChild(i);
                if (c && c.name.StartsWith(MarkerPrefix))
                    c.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Plot 컴포넌트를 사용하여 곡선 외곽선을 고려한 플롯 분할
        /// </summary>
        public List<Plot> DividePlot(Plot plot, int count)
        {
            if (plot?.PlotVertices == null || plot.PlotVertices.Count < 4) return null;
            if (count <= 0) return null;

            // 곡선 외곽선이 있으면 사용, 없으면 기본 정점 사용
            List<List<Vector3>> boundarySegments = plot.OutlineVertices?.Count > 0
                ? plot.OutlineVertices
                : new List<List<Vector3>> { plot.GetVertexPositions() };

            return DividePlotWithCurvedBoundary(plot, boundarySegments, count);
        }

        /// <summary>
        /// 곡선 경계를 고려한 플롯 분할 (기존 PlotVertex 정보 활용)
        /// </summary>
        private List<Plot> DividePlotWithCurvedBoundary(Plot plot, List<List<Vector3>> boundarySegments, int count)
        {
            if (plot?.PlotVertices == null || plot.PlotVertices.Count != 4) return null;
            if (boundarySegments == null || boundarySegments.Count == 0) return null;
            if (count <= 0) return null;

            var semiPlots = new List<Plot>(count);
            if (count == 1)
            {
                semiPlots.Add(plot);
                return semiPlots;
            }

            // 기존 PlotVertex 정보를 사용하여 4개 코너점 확보
            Vector3 corner0 = plot.GetVertexPosition(0); // 첫 번째 코너
            Vector3 corner1 = plot.GetVertexPosition(1); // 두 번째 코너
            Vector3 corner2 = plot.GetVertexPosition(2); // 세 번째 코너
            Vector3 corner3 = plot.GetVertexPosition(3); // 네 번째 코너

            // 앞뒤 변의 곡선 정보 확보 (분할 기준)
            List<Vector3> firstEdge = boundarySegments.Count > 0 && boundarySegments[0].Count > 1
                ? boundarySegments[0]
                : new List<Vector3> { corner0, corner1 };

            List<Vector3> thirdEdge = boundarySegments.Count > 2 && boundarySegments[2].Count > 1
                ? boundarySegments[2]
                : new List<Vector3> { corner2, corner3 };

            // 좌우 변의 곡선 정보 (첫 번째와 마지막 semi-plot에서만 사용)
            List<Vector3> secondEdge = boundarySegments.Count > 1 && boundarySegments[1].Count > 1
                ? boundarySegments[1]
                : new List<Vector3> { corner1, corner2 };

            List<Vector3> fourthEdge = boundarySegments.Count > 3 && boundarySegments[3].Count > 1
                ? boundarySegments[3]
                : new List<Vector3> { corner3, corner0 };

            // 곡선 세그먼트를 따라 분할된 semi-plot들을 생성
            for (int i = 0; i < count; i++)
            {
                float t1 = (float)i / count;
                float t2 = (float)(i + 1) / count;

                // 4개의 변을 각각 저장 (House는 4개 변 기대)
                List<List<Vector3>> fourEdges = new List<List<Vector3>>();

                // 1. 첫 번째 변의 곡선 세그먼트 (앞쪽, t1 ~ t2 구간)
                List<Vector3> frontEdgeSegment = GetCurveSegment(firstEdge, t1, t2);
                fourEdges.Add(new List<Vector3>(frontEdgeSegment));

                // 2. 오른쪽 변: 마지막 semi-plot(i == count-1)인 경우만 곡선 사용 가능
                Vector3 rightStart = frontEdgeSegment[frontEdgeSegment.Count - 1];
                Vector3 rightEnd = GetPointOnCurve(thirdEdge, 1.0f - t2);

                List<Vector3> rightEdgeSegment = new List<Vector3>();
                if (i == count - 1) // 마지막 semi-plot: 오른쪽이 외곽 경계
                {
                    // 두 번째 변(오른쪽)에서 연결 부분 추출
                    float rightT1 = GetTParameterForPoint(secondEdge, rightStart);
                    float rightT2 = GetTParameterForPoint(secondEdge, rightEnd);

                    if (rightT1 >= 0 && rightT2 >= 0 && Mathf.Abs(rightT2 - rightT1) > 0.001f)
                    {
                        // 곡선 세그먼트 사용
                        rightEdgeSegment = GetCurveSegment(secondEdge, rightT1, rightT2);
                    }
                    else
                    {
                        // 직선 사용
                        rightEdgeSegment = new List<Vector3> { rightStart, rightEnd };
                    }
                }
                else
                {
                    // 중간 semi-plot: 오른쪽은 항상 직선
                    rightEdgeSegment = new List<Vector3> { rightStart, rightEnd };
                }
                fourEdges.Add(rightEdgeSegment);

                // 3. 세 번째 변의 곡선 세그먼트 (뒤쪽, 역방향: t2 ~ t1 구간)
                List<Vector3> backEdgeSegment = GetCurveSegment(thirdEdge, 1.0f - t2, 1.0f - t1);
                fourEdges.Add(new List<Vector3>(backEdgeSegment));

                // 4. 왼쪽 변: 첫 번째 semi-plot(i == 0)인 경우만 곡선 사용 가능
                Vector3 leftStart = backEdgeSegment[backEdgeSegment.Count - 1];
                Vector3 leftEnd = frontEdgeSegment[0];

                List<Vector3> leftEdgeSegment = new List<Vector3>();
                if (i == 0) // 첫 번째 semi-plot: 왼쪽이 외곽 경계
                {
                    // 네 번째 변(왼쪽)에서 연결 부분 추출
                    float leftT1 = GetTParameterForPoint(fourthEdge, leftStart);
                    float leftT2 = GetTParameterForPoint(fourthEdge, leftEnd);

                    if (leftT1 >= 0 && leftT2 >= 0 && Mathf.Abs(leftT2 - leftT1) > 0.001f)
                    {
                        // 곡선 세그먼트 사용
                        leftEdgeSegment = GetCurveSegment(fourthEdge, leftT1, leftT2);
                    }
                    else
                    {
                        // 직선 사용
                        leftEdgeSegment = new List<Vector3> { leftStart, leftEnd };
                    }
                }
                else
                {
                    // 중간 semi-plot: 왼쪽은 항상 직선
                    leftEdgeSegment = new List<Vector3> { leftStart, leftEnd };
                }
                fourEdges.Add(leftEdgeSegment);

                // Plot 객체 생성 및 경계선 설정 (풀링 사용)
                GameObject semiPlotObject = GetOrCreateSemiPlotGameObject(i);
                Plot semiPlot = semiPlotObject.GetComponent<Plot>();
                if (semiPlot == null)
                {
                    semiPlot = semiPlotObject.AddComponent<Plot>();
                }
                semiPlot.InitializePlot();

                // 4개의 변을 각각 OutlineVertices에 설정
                semiPlot.UpdateOutlineVertices(fourEdges);

                semiPlots.Add(semiPlot);
            }

            // 사용하지 않는 기존 semi-plot GameObjects 비활성화
            DeactivateUnusedSemiPlots(count);

            return semiPlots;
        }

        /// <summary>
        /// 곡선에서 특정 점에 가장 가까운 위치의 t 매개변수를 찾습니다
        /// </summary>
        private float GetTParameterForPoint(List<Vector3> curvePoints, Vector3 targetPoint)
        {
            if (curvePoints == null || curvePoints.Count == 0) return -1f;
            if (curvePoints.Count == 1) return 0f;

            // 각 세그먼트의 누적 거리 계산
            List<float> cumulativeDistances = new List<float>();
            cumulativeDistances.Add(0f);

            float totalLength = 0f;
            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                float segmentLength = Vector3.Distance(curvePoints[i], curvePoints[i + 1]);
                totalLength += segmentLength;
                cumulativeDistances.Add(totalLength);
            }

            if (totalLength <= 0f) return 0f;

            // 가장 가까운 점과 세그먼트 찾기
            float closestDistance = float.MaxValue;
            float bestT = 0f;

            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                Vector3 segmentStart = curvePoints[i];
                Vector3 segmentEnd = curvePoints[i + 1];

                // 세그먼트에서 가장 가까운 점 찾기
                Vector3 segmentVector = segmentEnd - segmentStart;
                Vector3 pointVector = targetPoint - segmentStart;

                float segmentLength = segmentVector.magnitude;
                if (segmentLength < 0.001f) continue;

                float t = Mathf.Clamp01(Vector3.Dot(pointVector, segmentVector) / (segmentLength * segmentLength));
                Vector3 closestPointOnSegment = segmentStart + t * segmentVector;

                float distance = Vector3.Distance(targetPoint, closestPointOnSegment);
                if (distance < closestDistance)
                {
                    closestDistance = distance;

                    // 전체 곡선에서의 t 매개변수 계산
                    float segmentStartDistance = cumulativeDistances[i];
                    float segmentEndDistance = cumulativeDistances[i + 1];
                    float pointDistanceOnSegment = segmentStartDistance + t * (segmentEndDistance - segmentStartDistance);

                    bestT = pointDistanceOnSegment / totalLength;
                }
            }

            return bestT;
        }

        /// <summary>
        /// 곡선의 특정 구간(t1~t2)에 해당하는 점들을 추출합니다
        /// </summary>
        private List<Vector3> GetCurveSegment(List<Vector3> curvePoints, float t1, float t2)
        {
            if (curvePoints == null || curvePoints.Count == 0) return new List<Vector3>();
            if (curvePoints.Count == 1) return new List<Vector3> { curvePoints[0] };

            t1 = Mathf.Clamp01(t1);
            t2 = Mathf.Clamp01(t2);

            // t1과 t2가 같으면 단일 점 반환
            if (Mathf.Abs(t2 - t1) < 0.001f)
            {
                return new List<Vector3> { GetPointOnCurve(curvePoints, t1) };
            }

            // t1 > t2인 경우 swap
            if (t1 > t2)
            {
                float temp = t1;
                t1 = t2;
                t2 = temp;
            }

            // 직선인 경우 (2개 점만): 단순 보간
            if (curvePoints.Count == 2)
            {
                Vector3 start = Vector3.Lerp(curvePoints[0], curvePoints[1], t1);
                Vector3 end = Vector3.Lerp(curvePoints[0], curvePoints[1], t2);
                return new List<Vector3> { start, end };
            }

            List<Vector3> segment = new List<Vector3>();

            // 시작점 추가
            segment.Add(GetPointOnCurve(curvePoints, t1));

            // 각 세그먼트의 누적 거리 계산 (GetPointOnCurve와 동일한 로직)
            List<float> cumulativeDistances = new List<float>();
            cumulativeDistances.Add(0f);

            float totalLength = 0f;
            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                float segmentLength = Vector3.Distance(curvePoints[i], curvePoints[i + 1]);
                totalLength += segmentLength;
                cumulativeDistances.Add(totalLength);
            }

            if (totalLength <= 0f) return segment;

            // t1과 t2에 해당하는 거리 계산
            float startDistance = t1 * totalLength;
            float endDistance = t2 * totalLength;

            // 구간에 포함되는 중간 점들 추가
            for (int i = 0; i < curvePoints.Count; i++)
            {
                float pointDistance = cumulativeDistances[i];

                // 구간 범위 내에 있는 점들만 추가
                if (pointDistance > startDistance && pointDistance < endDistance)
                {
                    segment.Add(curvePoints[i]);
                }
            }

            // 끝점 추가 (시작점과 다른 경우에만)
            Vector3 endPoint = GetPointOnCurve(curvePoints, t2);
            if (Vector3.Distance(segment[segment.Count - 1], endPoint) > 0.001f)
            {
                segment.Add(endPoint);
            }

            return segment;
        }

        /// <summary>
        /// 곡선상의 t 위치(0~1)에서 점을 구합니다 (호장 길이 기준 분할)
        /// </summary>
        private Vector3 GetPointOnCurve(List<Vector3> curvePoints, float t)
        {
            if (curvePoints == null || curvePoints.Count == 0) return Vector3.zero;
            if (curvePoints.Count == 1) return curvePoints[0];

            t = Mathf.Clamp01(t);

            if (t <= 0f) return curvePoints[0];
            if (t >= 1f) return curvePoints[curvePoints.Count - 1];

            // 각 세그먼트의 누적 거리 계산
            List<float> cumulativeDistances = new List<float>();
            cumulativeDistances.Add(0f);

            float totalLength = 0f;
            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                float segmentLength = Vector3.Distance(curvePoints[i], curvePoints[i + 1]);
                totalLength += segmentLength;
                cumulativeDistances.Add(totalLength);
            }

            if (totalLength <= 0f) return curvePoints[0];

            // t에 해당하는 목표 거리 계산
            float targetDistance = t * totalLength;

            // 목표 거리가 위치한 세그먼트 찾기
            for (int i = 0; i < cumulativeDistances.Count - 1; i++)
            {
                if (targetDistance >= cumulativeDistances[i] && targetDistance <= cumulativeDistances[i + 1])
                {
                    // 해당 세그먼트 내에서의 로컬 t 값 계산
                    float segmentStartDistance = cumulativeDistances[i];
                    float segmentEndDistance = cumulativeDistances[i + 1];
                    float segmentLength = segmentEndDistance - segmentStartDistance;

                    if (segmentLength <= 0f) return curvePoints[i];

                    float localT = (targetDistance - segmentStartDistance) / segmentLength;
                    return Vector3.Lerp(curvePoints[i], curvePoints[i + 1], localT);
                }
            }

            // 예외 상황: 마지막 점 반환
            return curvePoints[curvePoints.Count - 1];
        }

        public void Clear()
        {
            ClearEdgeMarkersPreview();
            ClearSemiPlotPreview();
        }
        #endregion

        #region Private Methods
        private (int unitCount, float unitLength, float totalLength) CalculateOptimalEdgeMarkers(List<Vector3> plotVertices, float minLength, float maxLength)
        {
            if (plotVertices == null || plotVertices.Count < 2 || minLength <= 0f || maxLength <= 0f || maxLength < minLength)
                return (0, 0f, 0f);

            float total = (plotVertices[1] - plotVertices[0]).magnitude;
            if (total <= 0f) return (0, 0f, 0f);

            // 1) 최대 길이로 나누어떨어지면 그걸 우선 사용 (epsilon 비교)
            int maxCount = Mathf.FloorToInt(total / maxLength);
            if (maxCount > 0)
            {
                float rem = total - maxCount * maxLength;
                if (Mathf.Abs(rem) < 1e-3f) return (maxCount, maxLength, total);
            }

            // 2) 최소 길이 기준 가능한 최대 카운트 (세그 길이 <= maxLength 보장)
            int minCount = Mathf.FloorToInt(total / minLength);
            if (minCount > 0)
            {
                float seg = total / minCount;
                if (seg <= maxLength) return (minCount, seg, total);
            }

            return (0, 0f, total);
        }

        private void CreateOrUpdateEdgeMarkers(List<Vector3> plotVertices, int unitCount)
        {
            InitOnce();

            // 기존 마커 비활성화(재사용)
            for (int i = 0; i < _edgeMarkersParent.childCount; i++)
            {
                var c = _edgeMarkersParent.GetChild(i);
                if (c && c.name.StartsWith(MarkerPrefix))
                    c.gameObject.SetActive(false);
            }

            Vector3 start = plotVertices[0];
            Vector3 end   = plotVertices[1];
            Vector3 dir   = (end - start).normalized;
            Vector3 perp  = Vector3.Cross(dir, Vector3.up).normalized;

            for (int i = 1; i < unitCount; i++) // 양 끝 제외
            {
                float t = (float)i / unitCount;
                Vector3 pos = Vector3.Lerp(start, end, t);

                var name = $"{MarkerPrefix}{i}";
                var child = _edgeMarkersParent.Find(name);
                GameObject go;
                if (child) { go = child.gameObject; }
                else
                {
                    go = new GameObject(name);
                    go.transform.SetParent(_edgeMarkersParent, false);
                }

                go.SetActive(true);
                go.transform.position = pos;

                var lr = InitializeMarkerLineRenderer(go);

                Vector3 p0 = pos - perp * (markerLength * 0.5f); // 필드 사용(섀도잉 제거)
                Vector3 p1 = pos + perp * (markerLength * 0.5f);
                lr.SetPosition(0, p0);
                lr.SetPosition(1, p1);
            }
        }

        private LineRenderer InitializeMarkerLineRenderer(GameObject go)
        {
            var lr = go.GetComponent<LineRenderer>();
            if (!lr)
            {
                lr = go.AddComponent<LineRenderer>();
                lr.material      = lineMaterial;  // 새 머티리얼 생성 금지, 공유 사용
                lr.startWidth    = 0.1f;
                lr.endWidth      = 0.1f;
                lr.useWorldSpace = true;
                lr.loop          = false;
                lr.positionCount = 2;
            }
            lr.startColor = lr.endColor = markerColor;
            return lr;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t) return t.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>
        /// Semi-plot GameObject를 재사용하거나 새로 생성합니다 (풀링)
        /// </summary>
        private GameObject GetOrCreateSemiPlotGameObject(int index)
        {
            string semiPlotName = $"SemiPlot_{index}";
            Transform existingTransform = _semiPlotsParent.Find(semiPlotName);

            if (existingTransform != null)
            {
                // 기존 GameObject 재사용
                GameObject existingObj = existingTransform.gameObject;
                existingObj.SetActive(true);
                return existingObj;
            }

            // 새 GameObject 생성
            GameObject newSemiPlotObject = new GameObject(semiPlotName);
            newSemiPlotObject.transform.SetParent(_semiPlotsParent, false);
            return newSemiPlotObject;
        }

        /// <summary>
        /// 사용하지 않는 semi-plot GameObjects를 비활성화합니다
        /// </summary>
        private void DeactivateUnusedSemiPlots(int usedCount)
        {
            for (int i = usedCount; i < _semiPlotsParent.childCount; i++)
            {
                Transform child = _semiPlotsParent.GetChild(i);
                if (child != null && child.name.StartsWith("SemiPlot_"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
        #endregion
    }
}
