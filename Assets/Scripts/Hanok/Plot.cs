using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public class PlotVertex
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
            if (snapInfos == null || snapInfos.Count == 0) return Vector3.zero;

            var targetInfo = snapInfos.FirstOrDefault(info => info.snappedRoad == targetRoad);
            return targetInfo.snappedRoad != null ? targetInfo.snappedPosition :
                   snapInfos.FirstOrDefault().snappedPosition;
        }

        public SnapInfo GetSnapInfoForRoad(RoadComponent targetRoad)
        {
            if (snapInfos == null || targetRoad == null) return default(SnapInfo);

            return snapInfos.FirstOrDefault(info => info.snappedRoad == targetRoad);
        }

        public List<Vector3> GetAllPositions() => snapInfos?.Select(info => info.snappedPosition).ToList() ?? new List<Vector3>();

        public List<RoadComponent> GetAllRoads() => snapInfos?.Select(info => info.snappedRoad).ToList() ?? new List<RoadComponent>();
    }
    #endregion

    /// <summary>
    /// Plot 관련 공통 데이터와 유틸리티를 제공하는 클래스
    /// </summary>
    public class Plot : MonoBehaviour
    {
        #region Core Plot Properties
        [Header("Plot Data")]
        public List<PlotVertex> PlotVertices { get; private set; }
        public Mesh PlotMesh { get; private set; }
        public List<List<Vector3>> OutlineVertices { get; private set; }

        [Header("Debug Info")]
        [SerializeField] private List<PlotVertex> debugPlotVertices = new List<PlotVertex>();

        // Convenience Properties
        public List<Vector3> VertexPositions => GetVertexPositions();
        public List<bool> VertexSnapStates => ExtractSnapStates(PlotVertices);
        #endregion

        #region Initialization
        private void Awake()
        {
            InitializePlot();
        }

        public void InitializePlot()
        {
            PlotVertices = new List<PlotVertex>();
            OutlineVertices = new List<List<Vector3>>();
        }

        public void SetPlotMesh(Mesh mesh)
        {
            PlotMesh = mesh;
        }

        public void UpdateOutlineVertices(List<List<Vector3>> outlineSegments)
        {
            OutlineVertices = new List<List<Vector3>>();
            foreach (var segment in outlineSegments)
            {
                OutlineVertices.Add(new List<Vector3>(segment));
            }
        }

        /// <summary>
        /// 모든 외곽선 세그먼트를 하나의 리스트로 평면화
        /// </summary>
        public List<Vector3> GetFlattenedOutlineVertices()
        {
            var flattened = new List<Vector3>();
            if (OutlineVertices != null)
            {
                foreach (var segment in OutlineVertices)
                {
                    flattened.AddRange(segment);
                }
            }
            return flattened;
        }
        #endregion

        #region Vertex Management
        public void AddVertex(PlotVertex vertex)
        {
            if (PlotVertices == null) PlotVertices = new List<PlotVertex>();
            PlotVertices.Add(vertex);
            UpdateDebugInfo();
        }

        public void RemoveLastVertex()
        {
            if (PlotVertices != null && PlotVertices.Count > 0)
            {
                PlotVertices.RemoveAt(PlotVertices.Count - 1);
            }
            UpdateDebugInfo();
        }

        public void UpdateVertexAt(int index, PlotVertex vertex)
        {
            if (PlotVertices != null && index >= 0 && index < PlotVertices.Count)
            {
                PlotVertices[index] = vertex;
            }
            UpdateDebugInfo();
        }

        public void ClearVertices()
        {
            if (PlotVertices != null) PlotVertices.Clear();
            if (OutlineVertices != null) OutlineVertices.Clear();
            UpdateDebugInfo();
        }

        /// <summary>
        /// Inspector 디버깅을 위한 PlotVertex 정보 업데이트
        /// </summary>
        public void UpdateDebugInfo()
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

        #region Plot Utility Methods
        /// <summary>
        /// 현재 PlotVertices에서 위치값만 추출하여 반환
        /// </summary>
        public List<Vector3> GetVertexPositions()
        {
            if (PlotVertices == null || PlotVertices.Count == 0)
                return new List<Vector3>();

            List<Vector3> positions = new List<Vector3>(PlotVertices.Count);
            for (int i = 0; i < PlotVertices.Count; i++)
            {
                positions.Add(PlotVertices[i].GetAllPositions()[0]);
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

            return PlotVertices[index].GetAllPositions()[0];
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
        /// PlotVertex 리스트에서 위치값만 추출하여 반환 (Static 유틸리티)
        /// </summary>
        public List<Vector3> ExtractPositions(List<PlotVertex> plotVertices)
        {
            if (plotVertices == null || plotVertices.Count == 0)
                return new List<Vector3>();

            List<Vector3> positions = new List<Vector3>(plotVertices.Count);
            for (int i = 0; i < plotVertices.Count; i++)
            {
                positions.Add(plotVertices[i].GetAllPositions()[0]);
            }
            return positions;
        }

        /// <summary>
        /// PlotVertex 리스트에서 스냅 상태만 추출하여 반환
        /// </summary>
        public List<bool> ExtractSnapStates(List<PlotVertex> plotVertices)
        {
            if (plotVertices == null || plotVertices.Count == 0)
                return new List<bool>();

            return plotVertices.Select(v => v.isSnapped).ToList();
        }

        /// <summary>
        /// 주어진 인덱스의 PlotVertex가 스냅되어 있는지 확인
        /// </summary>
        public bool IsVertexSnappedAt(List<PlotVertex> plotVertices, int index)
        {
            if (plotVertices == null || index < 0 || index >= plotVertices.Count)
                return false;

            return plotVertices[index].isSnapped;
        }

        /// <summary>
        /// 주어진 인덱스의 PlotVertex 위치 반환
        /// </summary>
        public Vector3 GetVertexPosition(List<PlotVertex> plotVertices, int index)
        {
            if (plotVertices == null || index < 0 || index >= plotVertices.Count)
                return Vector3.zero;

            return plotVertices[index].GetAllPositions()[0];
        }
        #endregion
    }
}