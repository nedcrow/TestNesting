using UnityEngine;

/// <summary>
/// SceneView-like camera controller for Play Mode.
/// Attach to a Camera. Supports orbit / pan / zoom like the Unity Editor:
/// - Orbit:      Alt + LMB drag
/// - Pan:        MMB drag  (or Alt + MMB drag)
/// - Dolly:      Alt + RMB drag vertically
/// - Scroll:     Mouse wheel to zoom (distance-scaled)
/// - Focus:      F key raycasts from screen center and focuses pivot
/// - Speed:      Shift = fast, Ctrl = slow
/// </summary>
[DisallowMultipleComponent]
public class EditorLikeCameraController : MonoBehaviour
{
    [Header("Home")]
    public Vector3 home;

    [Header("Sensitivity")]
    [Tooltip("Degrees per pixel when orbiting.")]
    [SerializeField] private float orbitDegreesPerPixel = 0.25f;
    [Tooltip("World units per pixel for panning (scaled by distance).")]
    [SerializeField] private float panUnitsPerPixel = 0.0025f;
    [Tooltip("Zoom factor for scroll wheel (exponential). Higher is faster.")]
    [SerializeField] private bool allowPassThroughPivot = true;   // 피벗 통과 허용
    [SerializeField] private float passThroughMinDistance = 0.2f; // 이 거리로 클램프하고, 남은 양은 pivot을 전진
    [SerializeField] float zoomMul = 0.12f;     // 속도제어 (비례)
    [SerializeField] float zoomMinStep = 0.15f; // 근거리 최소 이동(m/스크롤틱)
    [Tooltip("Zoom factor for Alt+RMB vertical drag.")]
    [SerializeField] float dragDollyMul = 0.01f;
    [SerializeField] float dragDollyMinPerPixel = 0.002f; // 최소 m/pixel
    [SerializeField] private bool invertYOrbit = false;

    [Header("Distance")]
    [SerializeField] private float distance = 8f;
    [SerializeField] private float minDistance = 0.2f;
    [SerializeField] private float maxDistance = 1000f;

    [Header("Pitch Clamp (degrees)")]
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;

    [Header("Smoothing")]
    [Tooltip("0 = snap, 0.05~0.12 feels nice.")]
    [SerializeField] private float smoothTime = 0.08f;

    [Header("Focus")]
    [Tooltip("Layers to consider when pressing F to focus.")]
    [SerializeField] private LayerMask focusMask = ~0;
    [Tooltip("Fallback focus distance if nothing is hit by raycast.")]
    [SerializeField] private float fallbackFocusDistance = 8f;

    // Internal state
    private Vector3 pivot;               // The point we orbit around
    private float yaw;                   // Around world up
    private float pitch;                 // Around camera right
    private float targetDistance;
    private Vector3 targetPivot;
    private float targetYaw, targetPitch;

    // Velocities for smoothing
    private Vector3 pivotVel;
    private float yawVel, pitchVel, distanceVel;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        // Initialize angles from current camera rotation
        var e = transform.rotation.eulerAngles;
        yaw = targetYaw = NormalizeAngle(e.y);
        pitch = targetPitch = ClampPitch(NormalizeAngle(e.x));

        // Initialize pivot based on current forward & distance
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        pivot = targetPivot = transform.position + transform.forward * distance;
        targetDistance = distance;

        // Snap immediately to avoid initial smoothing lag
        ApplyTransform(snap: true);
    }

    void LateUpdate()
    {
        HandleInput();
        SmoothUpdate();
        ApplyTransform(snap: smoothTime <= 0f);
    }

    private void HandleInput()
    {
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        float speedMul = 1f;
        if (shift) speedMul *= 3f;
        if (ctrl) speedMul *= 0.33f;


        Vector2 md = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        // ORBIT: Alt + LMB
        if (alt && Input.GetMouseButton(0))
        {
            float signY = invertYOrbit ? 1f : -1f;
            targetYaw += md.x * orbitDegreesPerPixel * speedMul;
            targetPitch = ClampPitch(targetPitch + md.y * orbitDegreesPerPixel * signY * speedMul);
        }

        // PAN: MMB or Alt + MMB
        if (Input.GetMouseButton(2) || (alt && Input.GetMouseButton(2)))
        {
            // Pan in camera space, scaled by distance so pan feels consistent
            float s = panUnitsPerPixel * Mathf.Max(targetDistance, 0.01f) * speedMul;
            Vector3 right = GetRight(targetYaw, targetPitch);
            Vector3 up = GetUp(targetYaw, targetPitch);
            targetPivot += (-right * md.x + -up * md.y) * s;
        }

        // // DOLLY (drag): Alt + RMB vertical
        // if (alt && Input.GetMouseButton(1))
        // {
        //     float dist = Mathf.Max(targetDistance, 0.01f);
        //     float perPixel = Mathf.Max(dist * dragDollyMul, dragDollyMinPerPixel);
        //     float dd = -md.y * perPixel * speedMul;
        //     targetDistance = Mathf.Clamp(targetDistance + dd, minDistance, maxDistance);
        // }

        // // SCROLL ZOOM (exponential feel)
        // float scroll = Input.GetAxis("Mouse ScrollWheel");
        // if (Mathf.Abs(scroll) > Mathf.Epsilon)
        // {
        //     float dist = Mathf.Max(targetDistance, 0.01f);

        //     // 비례 스텝 + 최소 스텝 중 큰 값 사용
        //     float step = Mathf.Max(dist * zoomMul, zoomMinStep);

        //     // 스크롤 방향(보통 +가 zoom-in, -가 out인데 프로젝트에 따라 반대일 수 있음)
        //     float signed = (scroll > 0f) ? -step : +step;

        //     targetDistance = Mathf.Clamp(targetDistance + signed, minDistance, maxDistance);
        // }

        // DOLLY (drag): Alt + RMB vertical
        if (alt && Input.GetMouseButton(1))
        {
            float dist = Mathf.Max(targetDistance, 0.01f);
            float perPixel = Mathf.Max(dist * dragDollyMul * speedMul, dragDollyMinPerPixel * speedMul);

            // 마우스 위로 드래그(양수 md.y)는 보통 멀어짐으로, 아래로는 가까워짐으로 잡습니다.
            float dd = -md.y * perPixel; // 위(+y)->멀어짐(+), 아래(-y)->가까워짐(-)

            ApplyDolly(dd);
        }

        // SCROLL ZOOM (하이브리드 스텝: 비례 + 최소 보장)
        // Alt 키가 눌린 상태에서는 스크롤 줌 비활성화 (RoadBuilder의 Alt 곡선 조정과 충돌 방지)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (!alt && Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            float dist = Mathf.Max(targetDistance, 0.01f);
            float step = Mathf.Max(dist * zoomMul * speedMul, zoomMinStep * speedMul);

            // 휠 방향: 보통 +가 줌인(가까워짐)이라면 -step, 반대셋업이면 부호 반대로
            float signedDelta = (scroll > 0f ? -step : +step);

            ApplyDolly(signedDelta);
        }

        // FOCUS: F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            FocusAtCenter();
        }

        // HOME: H key
        if (Input.GetKeyDown(KeyCode.H))
        {
            MoveAtHome();
        }

        // Keep angles normalized
        targetYaw = NormalizeAngle(targetYaw);
        targetPitch = ClampPitch(targetPitch);
    }

    private void SmoothUpdate()
    {
        if (smoothTime <= 0f)
        {
            yaw = targetYaw;
            pitch = targetPitch;
            distance = targetDistance;
            pivot = targetPivot;
            return;
        }

        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVel, smoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVel, smoothTime);
        distance = Mathf.SmoothDamp(distance, targetDistance, ref distanceVel, smoothTime);
        pivot = Vector3.SmoothDamp(pivot, targetPivot, ref pivotVel, smoothTime);
    }

    private void ApplyDolly(float delta /* +이면 멀어짐, -이면 가까워짐 */)
    {
        float newDist = targetDistance + delta;

        if (allowPassThroughPivot && newDist < passThroughMinDistance)
        {
            // 얼마나 더 "안쪽"으로 가고 싶었는지
            float overshoot = passThroughMinDistance - newDist;

            // 현재 시선 방향으로 pivot을 전진시켜 '통과' 효과
            Vector3 fwd = GetForward(targetYaw, targetPitch);
            targetPivot += fwd * overshoot;

            // 카메라-피벗 거리는 최소 거리로 유지
            targetDistance = passThroughMinDistance;
        }
        else
        {
            targetDistance = Mathf.Clamp(newDist, minDistance, maxDistance);
        }
    }


    private void ApplyTransform(bool snap)
    {
        // Build rotation from yaw/pitch (yaw around world up, then pitch around local right)
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 camPos = pivot - (rot * Vector3.forward) * Mathf.Max(distance, minDistance);

        if (snap)
        {
            transform.position = camPos;
            transform.rotation = rot;
        }
        else
        {
            transform.SetPositionAndRotation(camPos, rot);
        }
    }

    private void FocusAtCenter()
    {
        Ray ray = cam != null
            ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0))
            : new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, focusMask, QueryTriggerInteraction.Ignore))
        {
            targetPivot = hit.point;
            // Set distance to frame the point nicely (not too close)
            targetDistance = Mathf.Clamp(Mathf.Max(minDistance, Vector3.Distance(transform.position, hit.point) * 0.8f), minDistance, maxDistance);
        }
        else
        {
            // Fallback: push pivot forward by a fixed distance
            Vector3 forward = GetForward(targetYaw, targetPitch);
            targetPivot = transform.position + forward * Mathf.Clamp(fallbackFocusDistance, minDistance, maxDistance);
            targetDistance = Mathf.Clamp(fallbackFocusDistance, minDistance, maxDistance);
        }
    }

    private void MoveAtHome()
    {
        // 원하는 홈 각도
        float homeYaw = 0f;
        float homePitch = ClampPitch(58f);

        // 현재(또는 타깃) 거리 유지
        float d = Mathf.Clamp(targetDistance, minDistance, maxDistance);

        // ApplyTransform 역산: camPos = pivot - rot*forward*d  ⇒  pivot = home + rot*forward*d
        Quaternion rot = Quaternion.Euler(homePitch, homeYaw, 0f);
        Vector3 newPivot = home + (rot * Vector3.forward) * d;

        // 1) 현재값 동기화
        yaw = NormalizeAngle(homeYaw);
        pitch = homePitch;
        distance = d;
        pivot = newPivot;

        // 2) 타깃값도 동일하게 맞춤(스무딩이 되돌리지 않도록)
        targetYaw = yaw;
        targetPitch = pitch;
        targetDistance = distance;
        targetPivot = pivot;

        // 3) 스무딩 속도 리셋(있다면)
        yawVel = pitchVel = distanceVel = 0f;
        pivotVel = Vector3.zero;
    }

    // --- Utility math ---
    private static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }

    private float ClampPitch(float p)
    {
        return Mathf.Clamp(p, minPitch, maxPitch);
    }

    private static Vector3 GetForward(float yawDeg, float pitchDeg)
    {
        Quaternion q = Quaternion.Euler(pitchDeg, yawDeg, 0f);
        return q * Vector3.forward;
    }

    private static Vector3 GetRight(float yawDeg, float pitchDeg)
    {
        Quaternion q = Quaternion.Euler(pitchDeg, yawDeg, 0f);
        return q * Vector3.right;
    }

    private static Vector3 GetUp(float yawDeg, float pitchDeg)
    {
        Quaternion q = Quaternion.Euler(pitchDeg, yawDeg, 0f);
        return q * Vector3.up;
    }

    // --- Public helpers ---
    /// <summary> Programmatically focus on a world position. </summary>
    public void FocusOnPosition(Vector3 worldPos, float desiredDistance = 5f)
    {
        targetPivot = worldPos;
        targetDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
    }

    /// <summary> Programmatically focus on a Transform (its position). </summary>
    public void FocusOnTransform(Transform t, float desiredDistance = 5f)
    {
        if (t == null) return;
        FocusOnPosition(t.position, desiredDistance);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        minDistance = Mathf.Max(0.001f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        minPitch = Mathf.Clamp(minPitch, -89.9f, 89.9f);
        maxPitch = Mathf.Clamp(maxPitch, -89.9f, 89.9f);
        if (maxPitch < minPitch) (minPitch, maxPitch) = (maxPitch, minPitch);
    }
#endif
}
