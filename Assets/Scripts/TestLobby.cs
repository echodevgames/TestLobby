using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using IngameDebugConsole;
using System.Threading.Tasks;
using System.Collections.Generic;


public class TestLobby : MonoBehaviour
{
    private static Lobby hostLobby;
    private static float heartbeatTimer;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };


        // Sign in Anon
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void Update()
    {
        //I am thinking about converting this to a coroutine
        HandleLobbyHeartbeat();


        if (Input.GetKeyDown(KeyCode.K))
        {
            CreateLobby();
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            ListLobbies();
        }

    }

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


    [ConsoleMethod("createlobby", "Creates a test lobby")]
    public static async void CreateLobby()
    {
        try
        {

            string lobbyName = "MyLobby";
            int maxPlayers = 1;
            

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);

            hostLobby = lobby;
            Debug.Log("Created Lobby! Name: " + lobby.Name + " Players: " + lobby.MaxPlayers);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    [ConsoleMethod("listLobby", "Lists lobbies")]
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
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
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




}
