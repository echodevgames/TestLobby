using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{
    private const string LobbySceneName = "LobbyScene";

    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private TMP_Text feedbackText;

    private async void Start()
    {
        await AuthManager.Instance.InitializeAsync();
    }

    /// <summary>Called by the Sign In button.</summary>
    public async void OnSignInPressed()
    {
        if (!ValidateInputs(out string username, out string password)) return;

        SetFeedback("Signing in...");

        try
        {
            await AuthManager.Instance.SignInAsync(username, password);
            SceneManager.LoadScene(LobbySceneName);
        }
        catch (Exception e)
        {
            SetFeedback($"Sign in failed: {e.Message}");
            Debug.LogError(e);
        }
    }

    /// <summary>Called by the Sign Up button.</summary>
    public async void OnSignUpPressed()
    {
        if (!ValidateInputs(out string username, out string password)) return;

        SetFeedback("Creating account...");

        try
        {
            await AuthManager.Instance.SignUpAsync(username, password);
            SceneManager.LoadScene(LobbySceneName);
        }
        catch (Exception e)
        {
            SetFeedback($"Sign up failed: {e.Message}");
            Debug.LogError(e);
        }
    }

    private bool ValidateInputs(out string username, out string password)
    {
        username = usernameInputField != null ? usernameInputField.text.Trim() : string.Empty;
        password = passwordInputField != null ? passwordInputField.text : string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            SetFeedback("Username cannot be empty.");
            username = string.Empty;
            password = string.Empty;
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            SetFeedback("Password cannot be empty.");
            return false;
        }

        return true;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }
}
