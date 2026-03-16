using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField lobbyCodeInputField;
    [SerializeField] private TMP_InputField gameModeInputField;
    [SerializeField] private TMP_Text currentGameModeText;
    [SerializeField] private TMP_Text lobbyInfoText;

    private void Start()
    {
        LobbyManager.Instance.OnLobbyCreated += HandleLobbyCreated;
        LobbyManager.Instance.OnLobbyJoined += HandleLobbyJoined;
        LobbyManager.Instance.OnLobbyUpdated += HandleLobbyUpdated;
        LobbyManager.Instance.OnLobbyLeft += HandleLobbyLeft;
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance == null) return;

        LobbyManager.Instance.OnLobbyCreated -= HandleLobbyCreated;
        LobbyManager.Instance.OnLobbyJoined -= HandleLobbyJoined;
        LobbyManager.Instance.OnLobbyUpdated -= HandleLobbyUpdated;
        LobbyManager.Instance.OnLobbyLeft -= HandleLobbyLeft;
    }

    // --- Button Handlers ---

    /// <summary>Called by the Dev Sign In button (SampleScene devbox only).</summary>
    public async void OnDevSignInPressed() => await AuthManager.Instance.SignInAnonymouslyAsync();

    /// <summary>Called by the Create Lobby button.</summary>
    public void OnCreateLobbyPressed() => LobbyManager.Instance.CreateLobby();

    /// <summary>Called by the Create Private Lobby button.</summary>
    public void OnCreatePrivateLobbyPressed() => LobbyManager.Instance.CreatePrivateLobby();

    /// <summary>Called by the List Lobbies button.</summary>
    public void OnListLobbiesPressed() => LobbyManager.Instance.ListLobbies();

    /// <summary>Called by the Join Lobby button.</summary>
    public void OnJoinLobbyPressed() => LobbyManager.Instance.JoinLobby();

    /// <summary>Called by the Quick Join button.</summary>
    public void OnQuickJoinPressed() => LobbyManager.Instance.QuickJoinLobby();

    /// <summary>Called by the Leave Lobby button.</summary>
    public void OnLeaveLobbyPressed() => LobbyManager.Instance.LeaveLobby();

    /// <summary>Called by the Delete Lobby button.</summary>
    public void OnDeleteLobbyPressed() => LobbyManager.Instance.DeleteLobby();

    /// <summary>Called by the Kick Player button.</summary>
    public void OnKickPlayerPressed() => LobbyManager.Instance.KickPlayer();

    /// <summary>Called by the Migrate Host button.</summary>
    public void OnMigrateHostPressed() => LobbyManager.Instance.MigrateLobbyHost();

    /// <summary>Called by the Join by Code button. Reads from the lobby code input field.</summary>
    public void OnJoinByCodePressed()
    {
        string code = lobbyCodeInputField != null ? lobbyCodeInputField.text : string.Empty;

        if (string.IsNullOrWhiteSpace(code))
        {
            Debug.LogWarning("Lobby code input is empty.");
            return;
        }

        LobbyManager.Instance.JoinLobbyByCode(code);
    }

    /// <summary>Called by the Update Game Mode button. Reads from the game mode input field.</summary>
    public void OnUpdateGameModePressed()
    {
        if (!LobbyManager.Instance.IsHost)
        {
            Debug.LogWarning("Only the host can update the game mode.");
            return;
        }

        string gameMode = gameModeInputField != null ? gameModeInputField.text : string.Empty;

        if (string.IsNullOrWhiteSpace(gameMode))
        {
            Debug.LogWarning("Game mode input is empty.");
            return;
        }

        LobbyManager.Instance.UpdateLobbyGameMode(gameMode);
    }

    // --- Event Handlers ---

    private void HandleLobbyCreated(Lobby lobby)
    {
        SetLobbyCodeDisplay(lobby.LobbyCode);
        RefreshLobbyInfo(lobby);
    }

    private void HandleLobbyJoined(Lobby lobby)
    {
        RefreshLobbyInfo(lobby);
    }

    private void HandleLobbyUpdated(Lobby lobby)
    {
        RefreshLobbyInfo(lobby);
    }

    private void HandleLobbyLeft()
    {
        if (lobbyCodeInputField != null) lobbyCodeInputField.text = string.Empty;
        if (currentGameModeText != null) currentGameModeText.text = string.Empty;
        if (lobbyInfoText != null) lobbyInfoText.text = string.Empty;
    }

    // --- Display Helpers ---

    private void SetLobbyCodeDisplay(string code)
    {
        if (lobbyCodeInputField == null) return;
        lobbyCodeInputField.text = code;
        lobbyCodeInputField.ForceLabelUpdate();
    }

    private void RefreshLobbyInfo(Lobby lobby)
    {
        if (currentGameModeText != null)
        {
            string mode = lobby.Data != null && lobby.Data.TryGetValue("GameMode", out DataObject gameModeData)
                ? gameModeData.Value
                : "—";
            currentGameModeText.text = $"MODE  —  {mode}";
        }

        if (lobbyInfoText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{lobby.Name}  ({lobby.Players.Count}/{lobby.MaxPlayers})");

        foreach (Player player in lobby.Players)
        {
            string name = player.Data != null && player.Data.TryGetValue("PlayerName", out PlayerDataObject nameData)
                ? nameData.Value
                : player.Id;

            sb.AppendLine($"  {name}");
        }

        lobbyInfoText.text = sb.ToString();
    }
}
