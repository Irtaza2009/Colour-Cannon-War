using TMPro;
using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingText;

    private void Awake()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    public void SetLoading(bool isLoading, string message = "Loading...")
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(isLoading);
        }

        if (loadingText != null)
        {
            loadingText.text = message;
        }
    }
}
