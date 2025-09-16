using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hanok
{
    public class PlotCreator : MonoBehaviour
    {
        #region Inspector Fields
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Material fillMaterial;
        [SerializeField] private float snapToRoadDistance = 2f;
        [SerializeField] private bool enableRoadSnapping = true;
        [SerializeField] private bool enableRoadCurving = true;
        #endregion

        #region Public Properties
        [Min(3)]
        public int MaxCountOfVertex { get; private set; } = 4;
        public List<Vector3> PlotVertices { get; private set; }
        public Mesh PlotMesh { get; private set; }
        public Color LineColor { get; set; } = Color.white;
        public Color FillColor { get; set; } = new Color(1f, 1f, 1f, 0.3f);
        #endregion

        #region Private Fields
        // Renderer components
        private LineRenderer lineRenderer;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        #endregion        

        #region Vertex Management
        public void InitializePlot()
        {
            PlotVertices = new List<Vector3>();
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
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
            
            // LineRenderer 초기 설정
            lineRenderer.material = lineMaterial;
            lineRenderer.startWidth = 0.4f;
            lineRenderer.endWidth = 0.4f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;
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

        public void AddVertex(Vector3 position)
        {
            if (PlotVertices == null) return;
            if (PlotVertices.Count >= MaxCountOfVertex) return;

            Vector3 snappedPosition = enableRoadSnapping ? SnapToRoad(position) : position;
            PlotVertices.Add(snappedPosition);
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

            Vector3 snappedPosition = enableRoadSnapping ? SnapToRoad(position) : position;
            PlotVertices[index] = snappedPosition;
            UpdatePlotVisualization();
        }

        public void RemoveLastVertex()
        {
            if (PlotVertices == null || PlotVertices.Count == 0) return;

            PlotVertices.RemoveAt(PlotVertices.Count - 1);
            UpdatePlotVisualization();
        }

        public void ClearPlot()
        {
            if (PlotVertices != null)
            {
                PlotVertices.Clear();
            }
            UpdatePlotVisualization();
        }
        
        #endregion

        #region Utility Methods
        private Vector3 SnapToRoad(Vector3 position)
        {
            return position;
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
            if (PlotVertices == null || PlotVertices.Count < 2 || lineRenderer == null) return;

            // 색상 업데이트
            lineRenderer.material.color = LineColor;
            lineRenderer.positionCount = PlotVertices.Count;

            // 점들을 LineRenderer에 설정
            for (int i = 0; i < PlotVertices.Count; i++)
            {
                lineRenderer.SetPosition(i, PlotVertices[i]);
            }

            // 닫힌 도형인 경우 (3개 이상의 점이 있을 때) 마지막 점을 첫 번째 점과 연결
            if (PlotVertices.Count >= 3)
            {
                lineRenderer.positionCount = PlotVertices.Count + 1;
                lineRenderer.SetPosition(PlotVertices.Count, PlotVertices[0]);
            }
        }

        private void ClearLineMesh()
        {
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
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
                vertices[i] = PlotVertices[i];
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
    }
}