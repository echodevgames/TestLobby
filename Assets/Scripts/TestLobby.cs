using IngameDebugConsole;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;



public class TestLobby : MonoBehaviour
{
    [SerializeField] private TMP_InputField lobbyCodeInputField;
    [SerializeField] private TMP_InputField gameModeInputField;
    [SerializeField] private TMP_Text currentGameModeText;


    public static TestLobby Instance { get; private set; }

    private static Lobby hostLobby;
    private static Lobby joinedLobby;
    private static string playerName;
    private Coroutine lobbyPollCoroutine;

    //courutine handles this
    //private static float heartbeatTimer;
    //going to also replace with coroutine
    //private static float lobbyUpdatetimer;

    private void Awake()
    {
        Instance = this;
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };


        // Sign in Anon
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        playerName = "CodeMonkey" + UnityEngine.Random.Range(10, 99);
        Debug.Log(playerName);

    }

    private void Update()
    {
        //Converted into a coroutine
        //HandleLobbyHeartbeat();
        //Also converting to a coroutine
        //HandleLobbyPollForUpdates();

        if (Input.GetKeyDown(KeyCode.K))
        {
            CreateLobby();
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            CreatePrivateLobby();
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            ListLobbies();
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            JoinLobby();
        }
        if(Input.GetKeyDown(KeyCode.U))
        {
            QuickJoinLobby();
        }


    }



    [ConsoleMethod("createlobby", "Creates Lobby")]
    public static async void CreateLobby()
    {
        try
        {

            string lobbyName = "MyLobby";
            int maxPlayers = 4;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "CaptureTheFlag")
                    },                   
                    {
                        "Map", new DataObject(DataObject.VisibilityOptions.Public, "de_dust2")
                    }
                }

            };



            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);


            hostLobby = lobby; 
            joinedLobby = lobby;

            Instance.StartCoroutine(Instance.LobbyPollForUpdatesCoroutine());
            Instance.StartCoroutine(Instance.LobbyHeartbeatCoroutine());

            if (Instance != null)
            {
                Instance.lobbyCodeInputField.text = lobby.LobbyCode;
                Instance.lobbyCodeInputField.ForceLabelUpdate();
            }



            Debug.Log("Created Lobby! Name: " + lobby.Name + " Players: " + lobby.MaxPlayers + " LobbyId: " + lobby.Id + " LobbyCode: " + lobby.LobbyCode);

            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    [ConsoleMethod("createPrivateLobby", "Creates Private Lobby")]
    public static async void CreatePrivateLobby()
    {
        try
        {

            string lobbyName = "MyPrivateLobby";
            int maxPlayers = 4;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = true,

                Player = GetPlayer()
            };



            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);


            hostLobby = lobby;
            joinedLobby = lobby;
            Instance.StartCoroutine(Instance.LobbyHeartbeatCoroutine());
            Instance.StartCoroutine(Instance.LobbyPollForUpdatesCoroutine());

            if (Instance != null)
            {
                Instance.lobbyCodeInputField.text = lobby.LobbyCode;
                Instance.lobbyCodeInputField.ForceLabelUpdate();
            }



            Debug.Log("Created Lobby! Name: " + lobby.Name + " Players: " + lobby.MaxPlayers + " LobbyId: " + lobby.Id + " LobbyCode: " + lobby.LobbyCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    [ConsoleMethod("listLobbies", "Lists Lobbies")]
    public static async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)

                }
            };

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);

            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " GameMode: " + lobby.Data["GameMode"].Value);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    [ConsoleMethod("joinLobby", "Join Lobby")]
    public static async void JoinLobby()
    {
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            if (queryResponse.Results.Count == 0)
            {
                Debug.Log("No lobbies available.");
                return;
            }

            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };

            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(
                queryResponse.Results[0].Id,
                joinLobbyByIdOptions
            );

            Instance.StartCoroutine(Instance.LobbyPollForUpdatesCoroutine());

            PrintPlayers(joinedLobby);



        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    [ConsoleMethod("joinLobbyByCode", "Join Lobby by Code")]
    public static async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions{Player = GetPlayer()};
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions); 
            joinedLobby = lobby;
            Instance.StartCoroutine(Instance.LobbyPollForUpdatesCoroutine());

            Debug.Log("Joined Lobby with code: " + lobbyCode);

            PrintPlayers(lobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    public void JoinLobbyByCodeFromUI()
    {
        string lobbyCode = lobbyCodeInputField.text;

        if (string.IsNullOrWhiteSpace(lobbyCode))
        {
            Debug.LogWarning("Lobby code is empty.");
            return;
        }

        JoinLobbyByCode(lobbyCode);
    }



    private IEnumerator LobbyHeartbeatCoroutine()
    {
        while (hostLobby != null)
        {
            yield return new WaitForSeconds(15f);
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
            }

            yield return new WaitForSeconds(1.1f);
        }
    }



    [ConsoleMethod("quickJoinLobby", "Quick Join Lobby")]
    public static async void QuickJoinLobby()
    {
        try
        {
            await LobbyService.Instance.QueryLobbiesAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private static Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
    }

    [ConsoleMethod("printPlayers", "Print Players")]
    public static void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Players in Lobby " + lobby.Name +
            " GameMode: " + lobby.Data["GameMode"].Value +
            " Map: " + lobby.Data["Map"].Value);

        foreach (Player player in lobby.Players)
        {
            Debug.Log(player.Id + " Player Name: " + player.Data["PlayerName"].Value);
        }

        if (Instance.currentGameModeText != null)
        {
            Instance.currentGameModeText.text = lobby.Data["GameMode"].Value;
        }
    }



    [ConsoleMethod("updateLobbyGameMode", "Update Lobby Game Mode")]
    public static async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                    {
                        {
                            "GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode)
                        }
                    }
            });
            joinedLobby = hostLobby;
            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public void UpdateLobbyGameModeFromUI()
    {
        if (hostLobby == null)
        {
            Debug.LogWarning("You must be the host to update GameMode.");
            return;
        }

        string gameMode = gameModeInputField.text;

        if (string.IsNullOrWhiteSpace(gameMode))
        {
            Debug.LogWarning("GameMode input is empty.");
            return;
        }

        UpdateLobbyGameMode(gameMode);
    }



    //Since Code monkey likes to use Quantum Console and I can't afford it, I went ahead here and made some alternative testing methods (partly for demonstrative purposes)

    //here weve made a function for pressing the "L" key to call the function

    //I've also added a UI button to the scene calling the function - this requires making the actual TestLobbyParent game object and attaching the script.

    //Button
    //→ OnClick()
    //→ Drag TestLobby object
    //→ Select CreateLobby()

    //And since I've been using the "InGameDebugConsole" to subtitute for "Quantum Console" 
    //they provide a similar method to code monkeys "[Command] attribute from QC, IGDC has 
    //[ConsoleMethod("createlobby", "Creates a test lobby")]

    //Now i can just call createlobby in the InGame Debug Console Asset

    /*
    //this may also be a coroutine?
    //He metions rate limits on polling and i think this might be why?
    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby != null)
        {
            lobbyUpdatetimer -= Time.deltaTime;
            if (lobbyUpdatetimer < 0f)
            {
                float lobbyUpdatetimerMax = 1.1f;
                lobbyUpdatetimer = lobbyUpdatetimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
            }
        }
    }



    //converted this to a coroutine
    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0)
            {
                float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    */


}
