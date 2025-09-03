using UnityEngine;
public class CameraSetup : MonoBehaviour
{    
    void Start()
    {
        Camera mainCam = Camera.main;

        if (mainCam != null)
        {
            mainCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            mainCam.orthographic = true;

            mainCam.orthographicSize = 10f;
        }
        else
        {
            Debug.LogWarning("Main Camera not found!");
        }
        NestingController.OnResizeEvent += ResetCamera;
    }
    void ResetCamera(Vector3 pos)
    {
        Camera.main.transform.position = pos;
        Camera.main.orthographicSize = pos.x * 1.24f;

    }

}
