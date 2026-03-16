using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public string PlayerName { get; private set; }
    public bool IsSignedIn => UnityServices.State == ServicesInitializationState.Initialized
                              && AuthenticationService.Instance.IsSignedIn;

    public event Action OnSignedIn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Initializes Unity Gaming Services. Safe to call multiple times.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
            return;

        await UnityServices.InitializeAsync();
    }

    /// <summary>
    /// Signs in an existing player with username and password.
    /// </summary>
    public async Task SignInAsync(string username, string password)
    {
        await InitializeAsync();
        await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
        PlayerName = username;
        OnSignedIn?.Invoke();
        Debug.Log($"Signed in: {PlayerName} ({AuthenticationService.Instance.PlayerId})");
    }

    /// <summary>
    /// Creates a new account and signs in with username and password.
    /// </summary>
    public async Task SignUpAsync(string username, string password)
    {
        await InitializeAsync();
        await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
        PlayerName = username;
        OnSignedIn?.Invoke();
        Debug.Log($"Signed up: {PlayerName} ({AuthenticationService.Instance.PlayerId})");
    }

    /// <summary>
    /// Signs in anonymously. Used by the SampleScene dev sandbox only.
    /// </summary>
    public async Task SignInAnonymouslyAsync()
    {
        await InitializeAsync();

        if (AuthenticationService.Instance.IsSignedIn)
            return;

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        PlayerName = "DevUser" + UnityEngine.Random.Range(10, 99);
        OnSignedIn?.Invoke();
        Debug.Log($"[DevBox] Signed in anonymously as {PlayerName}");
    }
}
