using TMPro;
using UnityEngine;

public class MultiplayerUI : MonoBehaviour
{
    [SerializeField] private MultiplayerManager multiplayerManager;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TMP_Text roomCodeText;

    public void ShowRoomCode(string code)
    {
        if (roomCodeText != null)
        {
            roomCodeText.text = "Room Code: " + code;
        }
    }
    public void JoinRoom()
    {
        if (multiplayerManager == null || roomCodeInput == null)
        {
            Debug.LogError("MultiplayerUI is missing a reference");
            return;
        }
        string roomCode = roomCodeInput.text.Trim().ToUpper();

        if(string.IsNullOrEmpty(roomCode))
        {
            Debug.LogWarning("Please enter a room code.");
            return;
        }

        multiplayerManager.JoinRoom(roomCode);
    }
}
