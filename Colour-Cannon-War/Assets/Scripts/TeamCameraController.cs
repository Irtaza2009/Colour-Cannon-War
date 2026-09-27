using UnityEngine;

public class TeamCameraController : MonoBehaviour
{
    public static TeamCameraController Instance { get; private set; }

    [SerializeField] private Camera blueCamera;
    [SerializeField] private Camera redCamera;

    private void Awake()
    {
        Instance = this;
        FindMissingCameras();
        DisableCameras();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool SetCameraForTeam(NetworkPlayer.Team team)
    {
        FindMissingCameras();
        DisableCameras();

        Camera teamCamera = team == NetworkPlayer.Team.Blue ? blueCamera : redCamera;

        if (teamCamera != null)
        {
            teamCamera.gameObject.SetActive(true);
            teamCamera.enabled = true;
            teamCamera.tag = "MainCamera";

            AudioListener audioListener = teamCamera.GetComponent<AudioListener>();

            if (audioListener != null)
            {
                audioListener.enabled = true;
            }

            return true;
        }

        return false;
    }

    private void FindMissingCameras()
    {
        if (blueCamera != null && redCamera != null)
        {
            return;
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);

        foreach (Camera camera in cameras)
        {
            string cameraName = camera.name.ToLowerInvariant();

            if (blueCamera == null && cameraName.Contains("blue"))
            {
                blueCamera = camera;
            }
            else if (redCamera == null && cameraName.Contains("red"))
            {
                redCamera = camera;
            }
        }
    }

    private void DisableCameras()
    {
        if (blueCamera != null)
        {
            blueCamera.enabled = false;
            DisableAudioListener(blueCamera);
            blueCamera.gameObject.SetActive(false);
        }

        if (redCamera != null)
        {
            redCamera.enabled = false;
            DisableAudioListener(redCamera);
            redCamera.gameObject.SetActive(false);
        }
    }

    private static void DisableAudioListener(Camera camera)
    {
        AudioListener audioListener = camera.GetComponent<AudioListener>();

        if (audioListener != null)
        {
            audioListener.enabled = false;
        }
    }
}
