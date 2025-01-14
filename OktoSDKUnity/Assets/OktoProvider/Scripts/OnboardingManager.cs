using OktoProvider;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OnboardingManager : MonoBehaviour
{
    [Header("WebView Configuration")]
    public Button openOnboardingButton;
    public Button closeWebViewButton;
    public enum AuthType { Email, Phone, GAuth }
    public AuthType authType = AuthType.Email;

    public float initialHeightPercentage = 0.97f;

    [Header("Dynamic Colors")]
    public string textPrimaryColor = "0xFFFFFFFF";
    public string textSecondaryColor = "0xFFFFFFFF";
    public string textTertiaryColor = "0xFFFFFFFF";
    public string accent1Color = "0xFF905BF5";
    public string accent2Color = "0x80905BF5";
    public string strokeBorderColor = "0xFFACACAB";
    public string strokeDividerColor = "0x4DA8A8A8";
    public string surfaceColor = "0xFF1F1F1F";
    public string backgroundColor = "0xFF000000";
    public string title = "OktoWalletTest";
    public string brandSubtitle = "Test it out";
    public string brandIconUrl = "";

    private WebViewObject webViewObject;

    private void Awake()
    {
        OktoProviderSDK.OnSDKInitialized += initOnboarding; // Subscribe to the event
        if (OktoProviderSDK.Instance != null) // If SDK is already initialized
        {
            initOnboarding();
        }
    }

    private void OnDestroy()
    {
        OktoProviderSDK.OnSDKInitialized -= initOnboarding; // Unsubscribe to avoid memory leaks
    }

    private string GetBuildUrl()
    {
        switch (OktoProviderSDK.Instance.buildStage)
        {
            case "SANDBOX":
                return "https://okto-sandbox.firebaseapp.com/#/login_screen"; 
            case "STAGING":
                return "https://3p.oktostage.com/#/login_screen";
            case "PRODUCTION":
                return "https://3p.okto.tech/login_screen/#/login_screen";
            default:
                return "";
        }
    }

    private void initOnboarding()
    {
        webViewObject = gameObject.AddComponent<WebViewObject>();

        webViewObject.Init(
            cb: (msg) => OnMessageReceivedAsync(msg),
            ld: (msg) =>
            {
                Debug.Log($"Page Loaded: {msg}");
                InjectJavaScript();
            }
        );
        string url = GetBuildUrl();
        webViewObject.LoadURL(url);
        webViewObject.SetMargins(0, 80, 0, 80);
        webViewObject.SetVisibility(false);
        if (openOnboardingButton != null)
            openOnboardingButton.onClick.AddListener(OpenOnboarding);
    }

    void InjectJavaScript()
    {
        string injectJs = $@"
    window.localStorage.setItem('ENVIRONMENT', '{OktoProviderSDK.Instance.buildStage}');
    window.localStorage.setItem('API_KEY', '{OktoProviderSDK.Instance.apiKey}');
    window.localStorage.setItem('textPrimaryColor', '{textPrimaryColor}');
    window.localStorage.setItem('textSecondaryColor', '{textSecondaryColor}');
    window.localStorage.setItem('textTertiaryColor', '{textTertiaryColor}');
    window.localStorage.setItem('accent1Color', '{accent1Color}');
    window.localStorage.setItem('accent2Color', '{accent2Color}');
    window.localStorage.setItem('strokeBorderColor', '{strokeBorderColor}');
    window.localStorage.setItem('strokeDividerColor', '{strokeDividerColor}');
    window.localStorage.setItem('surfaceColor', '{surfaceColor}');
    window.localStorage.setItem('backgroundColor', '{backgroundColor}');
    window.localStorage.setItem('primaryAuthType', '{authType}');
    window.localStorage.setItem('brandTitle', '{title}');
    window.localStorage.setItem('brandSubtitle', '{brandSubtitle}');
    window.localStorage.setItem('brandIconUrl', '{brandIconUrl}');
    ";
        string jsCode = @"
            (function() {
                // Save the original sendMessageToApp function
                const originalSendMessageToApp = window.sendMessageToApp;

                // Override sendMessageToApp
                window.sendMessageToApp = function(message) {
                    // Call Unity.call to send the message to Unity
                    if (typeof Unity !== 'undefined' && Unity.call) {
                        Unity.call(message);
                    }

                    // Call the original function (optional, if backend depends on it)
                    if (originalSendMessageToApp) {
                        originalSendMessageToApp(message);
                    }
                };
            })();
        ";
        injectJs += jsCode;
        webViewObject.EvaluateJS(injectJs);
       
    }

    public void OpenOnboarding()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        webViewObject.SetVisibility(true);
        if(closeWebViewButton != null)
        {
            closeWebViewButton.gameObject.SetActive(true);
            closeWebViewButton.onClick.AddListener(CloseWebView);
        }
    }
    private async Task OnMessageReceivedAsync(string message)
    {
        Debug.Log("Message Received: " + message);
        if (message.Contains("auth_success"))
        {
            MessageData messageVal = JsonUtility.FromJson<MessageData>(message);
            string authToken = messageVal.data.auth_token;
            string refreshAuthToken = messageVal.data.refresh_auth_token;
            OktoProviderSDK.Instance.AuthToken = authToken;
            OktoProviderSDK.Instance.DeviceToken = refreshAuthToken;
            Debug.Log($"Auth Success: {authToken}");
            OnLoginSuccess(authToken);
            CloseWebView();
        }
        else if (message.Contains("g_auth"))
        {
            AuthDetails authenticationData;
            Exception error;
            (authenticationData, error) = await OktoProviderSDK.Instance.LoginGoogle();

            if (error != null)
            {
                Debug.LogError($"Login failed with error: {error.Message}");
            }
            else
            {
                Debug.Log("Login successful!");
            }
            Debug.Log("loginDone " + authenticationData.authToken);
            OktoProviderSDK.Instance.AuthToken = authenticationData.authToken;
            OktoProviderSDK.Instance.DeviceToken = authenticationData.deviceToken;
            OktoProviderSDK.Instance.RefreshToken = authenticationData.refreshToken;
            CloseWebView();
        }
        else if (message.Contains("copy_text"))
        {
            string copiedText = GUIUtility.systemCopyBuffer; 
            SendCopiedTextToWebsite(copiedText);
        }
    }

    void SendCopiedTextToWebsite(string copiedText)
    {
        string jsCode = $@"
        if (window) {{
            window.postMessage(
                JSON.stringify({{
                    type: 'copy_text',
                    data: '{copiedText}'
                }}),
                '*'
            );
        }}
    ";

        webViewObject.EvaluateJS(jsCode);
    }

    private void OnLoginSuccess(string authToken)
    {
        Debug.Log("Login Success Callback Invoked " + authToken);
    }

    private void CloseWebView()
    {
        webViewObject.SetVisibility(false);
        Screen.orientation = ScreenOrientation.AutoRotation;
        if (closeWebViewButton != null)
        {
            closeWebViewButton.onClick.RemoveListener(CloseWebView);
            closeWebViewButton.gameObject.SetActive(false);
        }
    }

    [Serializable]
    public class AuthData
    {
        public string auth_token;
        public string refresh_auth_token;
    }

    [Serializable]
    public class MessageData
    {
        public string type;
        public AuthData data;
    }
}