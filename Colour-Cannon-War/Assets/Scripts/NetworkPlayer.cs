using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public enum Team
    {
        Blue,
        Red
    }

    public NetworkVariable<Team> PlayerTeam = new NetworkVariable<Team>();

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            PlayerTeam.Value = OwnerClientId == NetworkManager.ServerClientId
            ? Team.Blue
            : Team.Red;
        }
        //Workkkkkkk
        PlayerTeam.OnValueChanged += OnTeamChanged;

        UpdateAppearance(PlayerTeam.Value);

        if (IsOwner)
        {
            StartCoroutine(SetLocalCameraWhenReady(GetLocalTeam()));
        }
    }

    private void OnTeamChanged(Team previousTeam, Team newTeam)
    {
        UpdateAppearance(newTeam);

        if (IsOwner)
        {
            SetLocalCamera(newTeam);
        }
    }

    private void SetLocalCamera(Team team)
    {
        if (TeamCameraController.Instance != null)
        {
            TeamCameraController.Instance.SetCameraForTeam(team);
        }
    }

    private Team GetLocalTeam()
    {
        return OwnerClientId == NetworkManager.ServerClientId
            ? Team.Blue
            : Team.Red;
    }

    private System.Collections.IEnumerator SetLocalCameraWhenReady(Team team)
    {
        while (isActiveAndEnabled && IsSpawned)
        {
            if (TeamCameraController.Instance != null)
            {
                if (TeamCameraController.Instance.SetCameraForTeam(team))
                {
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void UpdateAppearance(Team team)
    {
        Renderer renderer = GetComponent<Renderer>();

        if (renderer == null)
        {
            return;
        }

        renderer.material.color = team == Team.Blue
            ? Color.blue
            : Color.red;
    }









}
