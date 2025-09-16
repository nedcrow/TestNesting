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
        private List<List<Vector3>> currentSemiPlots;

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
        public void ShowSemiPlotPreview(List<Vector3> plotVertices, int unitCount, Transform parent)
        {
            EnsureParents(parent);

            if (plotVertices == null || plotVertices.Count != 4 || unitCount <= 1)
            {
                ClearSemiPlotPreview();
                return;
            }

            currentSemiPlots = DividePlot(plotVertices, unitCount);
            if (currentSemiPlots == null)
            {
                ClearSemiPlotPreview();
                return;
            }

            VisualizeSemiPlots(currentSemiPlots);
        }

        public List<List<Vector3>> GetCurrentSemiPlots() => currentSemiPlots;

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

        private void VisualizeSemiPlots(List<List<Vector3>> semiPlots)
        {
            InitOnce();

            int needed = semiPlots.Count;

            // 1) 필요한 개수만큼 생성/갱신
            for (int i = 0; i < needed; i++)
            {
                var go = GetOrCreateChild(_semiPlotsParent, $"{SemiPlotPrefix}{i}");
                go.SetActive(true);

                var verts = semiPlots[i];
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
        public void ShowEdgeMarkersPreview(List<Vector3> plotVertices, float minUnitLength, float maxUnitLength, Transform parent)
        {
            EnsureParents(parent);

            if (plotVertices == null || plotVertices.Count < 2)
            {
                Clear();
                return;
            }

            var (unitCount, unitLen, totalLen) = CalculateOptimalEdgeMarkers(plotVertices, minUnitLength, maxUnitLength);
            if (unitCount <= 1)
            {
                Clear();
                return;
            }

            CreateOrUpdateEdgeMarkers(plotVertices, unitCount);

            if (plotVertices.Count == 4)
                ShowSemiPlotPreview(plotVertices, unitCount, parent);
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
        public List<List<Vector3>> DividePlot(List<Vector3> plotVertices, int count)
        {
            if (plotVertices == null || plotVertices.Count < 4) return null;
            if (count <= 0) return null;

            var semiPlots = new List<List<Vector3>>(count);

            // 첫 번째 라인 (0 -> 1), 세 번째 라인 (3 -> 2, 역방향)
            Vector3 a0 = plotVertices[0], a1 = plotVertices[1];
            Vector3 b0 = plotVertices[3], b1 = plotVertices[2];

            for (int i = 0; i < count; i++)
            {
                float t1 = (float)i / count;
                float t2 = (float)(i + 1) / count;

                Vector3 p1 = Vector3.Lerp(a0, a1, t1);
                Vector3 p2 = Vector3.Lerp(a0, a1, t2);
                Vector3 q1 = Vector3.Lerp(b0, b1, t1);
                Vector3 q2 = Vector3.Lerp(b0, b1, t2);

                semiPlots.Add(new List<Vector3> { p1, p2, q2, q1 }); // 시계방향
            }
            return semiPlots;
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
        #endregion
    }
}
