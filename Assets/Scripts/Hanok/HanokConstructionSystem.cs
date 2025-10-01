using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hanok
{
    public class HanokConstructionSystem : MonoBehaviour
    {
        #region instance
        private static HanokConstructionSystem _instance;
        public static HanokConstructionSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<HanokConstructionSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("HanokConstructionSystem");
                        _instance = go.AddComponent<HanokConstructionSystem>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Inspector References
        [Header("References")]
        [SerializeField] private PlotCreator plotCreator;
        [SerializeField] private PlotDivider plotDivider;
        [SerializeField] private HouseCreator houseCreator;
        [SerializeField] private LayerMask validLayer;
        #endregion


        #region Inspector Debug (Runtime State)
        [field: Header("Runtime Debug")]
        [field: SerializeField] public bool IsConstructionMode { get; private set; }
        [field: SerializeField] public bool IsPlotActive { get; private set; }
        [field: SerializeField] public bool IsCursorOnValidLayer { get; private set; }
        [field: SerializeField] public House CurrentHouse { get; private set; }

        #endregion

        #region Public Properties
        public float Unit { get; private set; } = 1;
        public List<Vector3> CurrentPlotVertices { get; private set; }
        public LayerMask ValidLayer
        {
            get => validLayer;
            set => validLayer = value;
        }

        public int CurrentCursorLayer { get; private set; } = -1;
        public Vector3 CurrentWorldPosition { get; private set; }
        #endregion

        #region Private Fields
        private Vector3 lastMousePosition;
        #endregion

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (IsConstructionMode)
            {
                HandleInput();
                CheckCursorLayerOnMove();
                ValidateConstruction();
            }
        }

        #region Input Handling
        private void HandleInput()
        {
            // Ignore input
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) ||
                Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Click
            if (IsCursorOnValidLayer)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    OnLeftClick();
                }

                if (Input.GetMouseButtonDown(1))
                {
                    OnRightClick();
                }

            }

            // Enter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnEnterPressed();
            }

            // Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnEscapePressed();
            }
        }

        private void OnLeftClick()
        {
            // Add vertex at cursor position
            if (plotCreator == null || CurrentWorldPosition == Vector3.zero) return;

            if (!IsPlotActive)
            {
                plotCreator.InitializePlot();
                // 첫 클릭에 두 개의 버텍스 추가 (시작점과 현재 마우스 위치)
                plotCreator.AddVertex(CurrentWorldPosition);
                plotCreator.AddVertex(CurrentWorldPosition);
                IsPlotActive = true;
            }
            else
            {
                // 이후 클릭에는 새로운 버텍스 추가
                plotCreator.AddVertex(CurrentWorldPosition);
            }
            CurrentPlotVertices = plotCreator.VertexPositions;
        }

        private void OnRightClick()
        {
            if (!IsPlotActive || plotCreator == null) return;

            plotCreator.RemoveLastVertex();
            CurrentPlotVertices = plotCreator.VertexPositions;

            switch (plotCreator.CurrentPlot.PlotVertices?.Count ?? 0)
            {
                case 3:
                    plotDivider.ClearSemiPlotPreview();
                    break;
                case 1:
                    IsPlotActive = false;
                    plotDivider.ClearEdgeMarkersPreview();
                    break;
            }
        }

        private void OnEnterPressed()
        {
            // Finalize current plot
            if (plotCreator == null || !IsPlotActive) return;
            if ((plotCreator.CurrentPlot.PlotVertices?.Count ?? 0) < 3) return;

            // TODO: Implement plot finalization logic
            IsPlotActive = false;
        }

        private void OnEscapePressed()
        {
            // Cancel current plot
            if (plotCreator == null) return;

            plotCreator.ClearPlot();
            if (plotDivider != null)
            {
                plotDivider.Clear();
            }
            CurrentPlotVertices = null;
            IsPlotActive = false;
        }


        private void CheckCursorLayerOnMove()
        {
            Vector3 currentMousePosition = Input.mousePosition;

            // 마우스가 움직였는지 확인
            if (Vector3.Distance(currentMousePosition, lastMousePosition) > 0.1f)
            {
                lastMousePosition = currentMousePosition;
                UpdateCursorLayerInfo();

                // 플롯이 활성상태일 때 마지막 버텍스 업데이트
                if (IsPlotActive && plotCreator != null && IsCursorOnValidLayer && CurrentWorldPosition != Vector3.zero)
                {
                    plotCreator.UpdateVertexPosition(CurrentWorldPosition);
                    CurrentPlotVertices = plotCreator.VertexPositions;
                    
                    // 2개 이상 버텍스일 때 분할 마커 표시
                    // (2개: 마커만, 3개: 마커만, 4개: 마커+semiPlot)
                    if (CurrentHouse != null && (plotCreator.CurrentPlot.PlotVertices?.Count ?? 0) >= 2 && plotDivider != null)
                    {
                        plotDivider.ShowEdgeMarkersPreview(plotCreator.CurrentPlot, CurrentHouse.MinimumLength, CurrentHouse.MaximumLength, plotCreator.transform);
                    }
                    else
                    {
                        if (plotDivider != null)
                        {
                            plotDivider.Clear();
                        }
                    }
                }
                else if (IsPlotActive && plotCreator != null)
                {
                    // 유효하지 않은 레이어에 있을 때 모든 미리보기 정리
                    if (plotDivider != null)
                    {
                        plotDivider.Clear();
                    }
                }
            }
        }

        private void UpdateCursorLayerInfo()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                CurrentCursorLayer = hit.collider.gameObject.layer;
                IsCursorOnValidLayer = ((1 << CurrentCursorLayer) & validLayer) != 0;
                CurrentWorldPosition = hit.point;
            }
            else
            {
                CurrentCursorLayer = -1;
                IsCursorOnValidLayer = false;
                CurrentWorldPosition = Vector3.zero;
            }
        }
        #endregion

        #region Public Methods
        public void StartConstructionMode()
        {
            IsConstructionMode = true;
            if (plotCreator != null)
            {
                plotCreator.InitializePlot();
            }
            InitializePlotDivider();
        }
        
        private void InitializePlotDivider()
        {
            if (plotDivider == null)
            {
                plotDivider = GetComponent<PlotDivider>();
                if (plotDivider == null)
                {
                    plotDivider = gameObject.AddComponent<PlotDivider>();
                }
            }
        }

        public void StopConstructionMode()
        {
            IsConstructionMode = false;
            IsPlotActive = false;
            if (plotCreator != null)
            {
                plotCreator.ClearPlot();
            }
            if (plotDivider != null)
            {
                plotDivider.Clear();
            }
            CurrentPlotVertices = null;
        }
        #endregion

        #region Construction Validation
        private void ValidateConstruction()
        {
            // ValidateBuilding();
            // 각 집의 필수 건물 배치 가능성 검증 (크기 제약, 우선순위 고려)
            // 리소스 비용 검증 (건설에 필요한 자원 보유 여부)
            // 도로 연결 요구사항 검증 (스냅 거리 내 도로 존재 여부)
            // 다른 구조물과의 충돌 검증 (기존 건물, 집과의 겹침 방지)
            // UI 피드백 업데이트 (유효/무효 상태 시각적 표시)
        }

        private void ValidateBuilding()
        {

        }
        #endregion
    }
}