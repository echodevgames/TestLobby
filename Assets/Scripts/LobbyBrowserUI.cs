using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyBrowserUI : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyListText;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text chatText;

    private void Start()
    {
        LobbyManager.Instance.OnLobbiesListed += HandleLobbiesListed;
        LobbyManager.Instance.OnLobbyJoined += HandleLobbyJoined;

        LobbyManager.Instance.ListLobbies();
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance == null) return;

        LobbyManager.Instance.OnLobbiesListed -= HandleLobbiesListed;
        LobbyManager.Instance.OnLobbyJoined -= HandleLobbyJoined;
    }

    /// <summary>Called by the Refresh button.</summary>
    public void OnRefreshPressed() => LobbyManager.Instance.ListLobbies();

    /// <summary>Called by the Quick Join button.</summary>
    public void OnQuickJoinPressed() => LobbyManager.Instance.QuickJoinLobby();

    /// <summary>Called by the Join by Code button.</summary>
    public void OnJoinByCodePressed()
    {
        string code = joinCodeInputField != null ? joinCodeInputField.text.Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(code))
        {
            SetFeedback("Enter a lobby code.");
            return;
        }

        LobbyManager.Instance.JoinLobbyByCode(code);
    }

    private void HandleLobbiesListed(List<Lobby> lobbies)
    {
        if (lobbyListText == null) return;

        if (lobbies.Count == 0)
        {
            lobbyListText.text = "No lobbies available.";
            return;
        }

        StringBuilder sb = new StringBuilder();

        foreach (Lobby lobby in lobbies)
        {
            string gameMode = lobby.Data != null && lobby.Data.TryGetValue("GameMode", out DataObject data)
                ? data.Value
                : "—";

            sb.AppendLine($"{lobby.Name}  |  {gameMode}  |  {lobby.Players.Count}/{lobby.MaxPlayers}");
        }

        lobbyListText.text = sb.ToString();
    }

    private void HandleLobbyJoined(Lobby lobby)
    {
        SetFeedback($"Joined: {lobby.Name}");
        // TODO: transition to waiting room or game scene
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }
}
