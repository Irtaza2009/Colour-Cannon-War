using TMPro;
using UnityEngine;

public class MultiplayerUI : MonoBehaviour
{
    [SerializeField] private MultiplayerManager multiplayerManager;
    [SerializeField] private GameObject roomCodePanel;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TMP_Text roomCodeText;

    private void Awake()
    {
        HideRoomCode();

        if (roomCodeInput != null)
        {
            roomCodeInput.onValueChanged.AddListener(KeepRoomCodeUppercase);
        }
    }

    private void OnDestroy()
    {
        if (roomCodeInput != null)
        {
            roomCodeInput.onValueChanged.RemoveListener(KeepRoomCodeUppercase);
        }
    }

    public void ShowRoomCode(string code)
    {
        if (roomCodePanel != null)
        {
            roomCodePanel.SetActive(true);
        }

        if (roomCodeText != null)
        {
            roomCodeText.text = "Room Code: " + code;
        }
    }

    private void HideRoomCode()
    {
        if (roomCodePanel != null)
        {
            roomCodePanel.SetActive(false);
        }
        else if (roomCodeText != null)
        {
            roomCodeText.gameObject.SetActive(false);
        }
    }

    public void JoinRoom()
    {
        if (multiplayerManager == null || roomCodeInput == null)
        {
            Debug.LogError("MultiplayerUI is missing a reference");
            return;
        }
        string roomCode = roomCodeInput.text.Trim().ToUpperInvariant();

        if(string.IsNullOrEmpty(roomCode))
        {
            Debug.LogWarning("Please enter a room code.");
            return;
        }

        multiplayerManager.JoinRoom(roomCode);
    }

    private void KeepRoomCodeUppercase(string value)
    {
        string uppercaseValue = value.ToUpperInvariant();

        if (value != uppercaseValue)
        {
            roomCodeInput.SetTextWithoutNotify(uppercaseValue);
            roomCodeInput.caretPosition = uppercaseValue.Length;
        }
    }
}
