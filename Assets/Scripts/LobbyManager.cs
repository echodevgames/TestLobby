using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private const float HeartbeatIntervalSeconds = 15f;
    private const float PollIntervalSeconds = 1.1f;
    private const int MaxLobbyResults = 25;
    private const int MaxLobbyPlayers = 4;
    private const string DefaultLobbyName = "MyLobby";
    private const string DefaultPrivateLobbyName = "MyPrivateLobby";
    private const string DefaultGameMode = "CaptureTheFlag";
    private const string DefaultMap = "de_dust2";

    private Lobby hostLobby;
    private Lobby joinedLobby;

    public Lobby JoinedLobby => joinedLobby;
    public Lobby HostLobby => hostLobby;
    public bool IsHost => hostLobby != null;

    public event Action<Lobby> OnLobbyCreated;
    public event Action<Lobby> OnLobbyJoined;
    public event Action<Lobby> OnLobbyUpdated;
    public event Action<List<Lobby>> OnLobbiesListed;
    public event Action OnLobbyLeft;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private async void Start()
    {
        await AuthManager.Instance.InitializeAsync();
        // Sign-in is handled by LoginUI (production) or LobbyUI dev sign-in (SampleScene).
    }

    /// <summary>
    /// Creates a public lobby with default GameMode and Map data.
    /// </summary>
    public async void CreateLobby()
    {
        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = BuildPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, DefaultGameMode) },
                    { "Map", new DataObject(DataObject.VisibilityOptions.Public, DefaultMap) }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(DefaultLobbyName, MaxLobbyPlayers, options);
            hostLobby = lobby;
            joinedLobby = lobby;

            StartCoroutine(LobbyHeartbeatCoroutine());
            StartCoroutine(LobbyPollForUpdatesCoroutine());

            Debug.Log($"Created lobby: {lobby.Name} | Code: {lobby.LobbyCode} | Max Players: {lobby.MaxPlayers}");
            OnLobbyCreated?.Invoke(lobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Creates a private lobby accessible only via lobby code.
    /// </summary>
    public async void CreatePrivateLobby()
    {
        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = true,
                Player = BuildPlayer()
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(DefaultPrivateLobbyName, MaxLobbyPlayers, options);
            hostLobby = lobby;
            joinedLobby = lobby;

            StartCoroutine(LobbyHeartbeatCoroutine());
            StartCoroutine(LobbyPollForUpdatesCoroutine());

            Debug.Log($"Created private lobby: {lobby.Name} | Code: {lobby.LobbyCode}");
            OnLobbyCreated?.Invoke(lobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Queries and broadcasts available lobbies with open slots.
    /// </summary>
    public async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = MaxLobbyResults,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            Debug.Log($"Lobbies found: {response.Results.Count}");
            OnLobbiesListed?.Invoke(response.Results);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Joins the first available public lobby by ID.
    /// </summary>
    public async void JoinLobby()
    {
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
            if (response.Results.Count == 0)
            {
                Debug.Log("No lobbies available.");
                return;
            }

            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions { Player = BuildPlayer() };
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(response.Results[0].Id, options);

            StartCoroutine(LobbyPollForUpdatesCoroutine());

            Debug.Log($"Joined lobby: {joinedLobby.Name}");
            OnLobbyJoined?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Joins a lobby using the provided lobby code.
    /// </summary>
    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions { Player = BuildPlayer() };
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);

            StartCoroutine(LobbyPollForUpdatesCoroutine());

            Debug.Log($"Joined lobby with code: {lobbyCode}");
            OnLobbyJoined?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Joins the best available lobby without requiring a specific ID or code.
    /// </summary>
    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions { Player = BuildPlayer() };
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

            StartCoroutine(LobbyPollForUpdatesCoroutine());

            Debug.Log($"Quick joined lobby: {joinedLobby.Name}");
            OnLobbyJoined?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Updates the game mode of the hosted lobby. Only valid if the local player is the host.
    /// </summary>
    public async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode) }
                }
            });

            joinedLobby = hostLobby;
            OnLobbyUpdated?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Updates the local player's display name in the current lobby.
    /// </summary>
    public async void UpdatePlayerName(string newPlayerName)
    {
        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(
                joinedLobby.Id,
                AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newPlayerName) }
                    }
                });

            Debug.Log($"Player name updated to: {newPlayerName}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Removes the local player from the current lobby.
    /// </summary>
    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            ClearLobbyState();
            OnLobbyLeft?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Kicks the second player in the lobby. Only valid if the local player is the host.
    /// </summary>
    public async void KickPlayer()
    {
        try
        {
            if (joinedLobby.Players.Count < 2)
            {
                Debug.LogWarning("No other players to kick.");
                return;
            }

            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[1].Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Transfers host privileges to the second player in the lobby.
    /// </summary>
    public async void MigrateLobbyHost()
    {
        try
        {
            if (joinedLobby.Players.Count < 2)
            {
                Debug.LogWarning("Not enough players to migrate host.");
                return;
            }

            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = joinedLobby.Players[1].Id
            });

            joinedLobby = hostLobby;
            OnLobbyUpdated?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Deletes the current lobby. Only valid if the local player is the host.
    /// </summary>
    public async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
            ClearLobbyState();
            OnLobbyLeft?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private IEnumerator LobbyHeartbeatCoroutine()
    {
        while (hostLobby != null)
        {
            yield return new WaitForSeconds(HeartbeatIntervalSeconds);
            LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
        }
    }

    private IEnumerator LobbyPollForUpdatesCoroutine()
    {
        while (joinedLobby != null)
        {
            var task = LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
            yield return new WaitUntil(() => task.IsCompleted);

            if (!task.IsFaulted && !task.IsCanceled)
            {
                joinedLobby = task.Result;
                OnLobbyUpdated?.Invoke(joinedLobby);
            }

            yield return new WaitForSeconds(PollIntervalSeconds);
        }
    }

    private Player BuildPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthManager.Instance.PlayerName) }
            }
        };
    }

    private void ClearLobbyState()
    {
        hostLobby = null;
        joinedLobby = null;
    }
}
