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
    }

    private void OnTeamChanged(Team previousTeam, Team newTeam)
    {
        UpdateAppearance(newTeam);
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
