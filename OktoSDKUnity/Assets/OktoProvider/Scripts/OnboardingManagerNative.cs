using OktoProvider;
using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OnboardingManagerNative : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button Google;
    [SerializeField] private Button Google1;
    [SerializeField] private Button Mail;
    [SerializeField] private Button Mail1;
    [SerializeField] private Button Phone;
    [SerializeField] private Button Phone1;
    [SerializeField] private Button sendOTP;
    [SerializeField] private Button sendCode;
    [SerializeField] private Button googleAccount;
    [SerializeField] private Button paste;
    [SerializeField] private Button resendOTP;
    [SerializeField] private Button backButtonMain;
    [SerializeField] private Button backButtonConfirm;

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField phoneNumber;
    [SerializeField] private TMP_InputField emailAddress;
    public TMP_InputField hiddenInputField;
    public TextMeshProUGUI[] digitFields;

    [Header("GameObjects")]
    [SerializeField] private GameObject defaultObjects;
    [SerializeField] private GameObject phoneScreen;
    [SerializeField] private GameObject emailScreen;
    [SerializeField] private GameObject googleScreen;
    [SerializeField] private GameObject confirmationScreen;
    [SerializeField] private GameObject onboardingScreen;
    [SerializeField] private GameObject IncorrectCode;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI timeCounter;
    [SerializeField] private TextMeshProUGUI verificationText;

    [Header("Sprites")]
    [SerializeField] private Sprite codeDefault;
    [SerializeField] private Sprite codeTyping;
    [SerializeField] private Sprite codeSuccessful;
    [SerializeField] private Sprite codeIncorrect;
    [SerializeField] private Sprite logoEmail;
    [SerializeField] private Sprite logoPhone;

    [SerializeField] private Image logoImage;

    private string emailToken;
    private string phoneToken;

    private bool isEmailAuth = false;

    private float resendCooldown = 30f;
    private float timer = 0f;

    public string otpString = "";

    private int currentIndex = 0;
    private string previousInput = "";

    private Color defaultColor = new Color32(112, 112, 112, 255);
    private Color activeColor = new Color32(84, 102, 238, 255);
    private void Start()
    {
        Google.onClick.AddListener(OpenGoogleScreen);
        Phone.onClick.AddListener(OpenPhoneScreen);
        Mail.onClick.AddListener(OpenMailScreen);
        Google1.onClick.AddListener(OpenGoogleScreen);
        Phone1.onClick.AddListener(OpenPhoneScreen);
        Mail1.onClick.AddListener(OpenMailScreen);
        paste.onClick.AddListener(PasteCode);
        resendOTP.onClick.AddListener(ResendOTP);
        sendCode.onClick.AddListener(SendEmailOtp);
        sendOTP.onClick.AddListener(SendPhoneOtp);
        googleAccount.onClick.AddListener(GoogleLoginAsync);
        backButtonConfirm.onClick.AddListener(backToMain);
        backButtonMain.onClick.AddListener(backToHome);

        hiddenInputField.onValueChanged.AddListener(HandleInput);
        hiddenInputField.onSelect.AddListener(UpdateHighlight);

        phoneNumber.onValueChanged.AddListener(ValidatePhoneNumber);
        emailAddress.onValueChanged.AddListener(ValidateEmailAddress);
    }
    private void OnEnable()
    {
        isEmailAuth = false;
    }

    private void ValidatePhoneNumber(string phone)
    {
        if (phone.Length > 10)
        {
            phoneNumber.text = phone.Substring(0, 10);
        }

        sendOTP.interactable = phoneNumber.text.Length == 10;
    }

    private void ValidateEmailAddress(string email)
    {
        bool isValidEmail = !string.IsNullOrEmpty(email) && email.Contains("@") && email.Contains(".");
        sendCode.interactable = isValidEmail;
    }

    private async void GoogleLoginAsync()
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
        CloseAllScreens();
        phoneScreen.SetActive(true);
        defaultObjects.SetActive(true);
        clearCodeField();
        onboardingScreen.SetActive(false);
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            timeCounter.text = $"({Mathf.Ceil(timer)} sec)";
            resendOTP.interactable = false;
            resendOTP.transform.GetComponentInChildren<TextMeshProUGUI>().color = defaultColor;
        }
        else
        {
            timer = 0;
            timeCounter.text = "";
            resendOTP.interactable = true;
            resendOTP.transform.GetComponentInChildren<TextMeshProUGUI>().color = activeColor;
        }
    }
    private void backToHome()
    {
        clearInputFields();
        CloseAllScreens();
        IncorrectCode.SetActive(false);
        phoneScreen.SetActive(true);
        onboardingScreen.SetActive(false);
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }
    private void backToMain()
    {
        clearInputFields();
        clearCodeField();
        IncorrectCode.SetActive(false);
        CloseAllScreens();
        phoneScreen.SetActive(true);
        defaultObjects.SetActive(true);
    }

    private void OpenGoogleScreen()
    {
        clearInputFields();
        CloseAllScreens();
        googleScreen.SetActive(true);
    }

    private void OpenPhoneScreen()
    {
        clearInputFields();
        CloseAllScreens();
        phoneScreen.SetActive(true);
        isEmailAuth = false;
    }

    private void OpenMailScreen()
    {
        clearInputFields();
        CloseAllScreens();
        emailScreen.SetActive(true);
        isEmailAuth = true;
    }

    private void CloseAllScreens()
    {
        phoneScreen.SetActive(false);
        emailScreen.SetActive(false);
        googleScreen.SetActive(false);
        confirmationScreen.SetActive(false);
    }

    private void PasteCode()
    {
        string clipboard = GUIUtility.systemCopyBuffer;
        if (clipboard.Length == 6)
        {
            for (int i = 0; i < 6; i++)
            {
                digitFields[i].text = clipboard[i].ToString();
            }
            currentIndex = 5;
            hiddenInputField.text = clipboard;
            CheckCodeAsync();
        }

    }

    void HandleInput(string currentInput)
    {

        for (int i = 0; i < digitFields.Length; i++)
        {
            digitFields[i].text = "";
        }

        for (int i = 0; i < currentInput.Length && i < digitFields.Length; i++)
        {
            digitFields[i].text = currentInput[i].ToString();
        }

        currentIndex = Mathf.Clamp(currentInput.Length, 0, digitFields.Length - 1);

        UpdateHighlight();

        if (currentInput.Length == 6)
        {
            _ = CheckCodeAsync();
        }

        previousInput = currentInput;
    }

    void UpdateHighlight(string argument = null)
    {
        if (onboardingScreen.activeInHierarchy)
        {
            foreach (var field in digitFields)
            {
                Image fieldImage = field.transform.GetComponentInParent<Image>();
                fieldImage.sprite = codeDefault;
            }
            HighlightSelectedField(currentIndex);
        }
    }

    private void HighlightSelectedField(int index)
    {
        Image fieldImage = digitFields[index].transform.GetComponentInParent<Image>();
        fieldImage.sprite = codeTyping;
    }

    private bool AllFieldsFilled(TMP_InputField[] codeFields)
    {
        foreach (var field in codeFields)
        {
            if (string.IsNullOrEmpty(field.text))
            {
                return false;
            }
        }
        return true;
    }

    private void clearCodeField()
    {
        currentIndex = 0;
        hiddenInputField.text = "";
        foreach (var field in digitFields)
        {
            field.text = "";
            Image fieldImage = field.transform.GetComponentInParent<Image>();
            fieldImage.sprite = codeDefault;
        }
    }
    private void clearInputFields()
    {
        phoneNumber.text = "";
        emailAddress.text = "";
    }
    private void successCodeField()
    {
        foreach (var field in digitFields)
        {
            Image fieldImage = field.transform.GetComponentInParent<Image>();
            fieldImage.sprite = codeSuccessful;
        }
    }
    private void failedCodeField()
    {
        foreach (var field in digitFields)
        {
            Image fieldImage = field.transform.GetComponentInParent<Image>();
            fieldImage.sprite = codeIncorrect;
        }
    }

    private async Task CheckCodeAsync()
    {
        Debug.Log("Checking");
        bool verified;
        if (isEmailAuth)
        {
            verified = await VerifyEmailOtp(hiddenInputField.text);
            IncorrectCode.SetActive(!verified);
            if (verified)
            {
                successCodeField();
            }
            else
            {
                failedCodeField();
            }

        }
        else
        {
            verified = await VerifyPhoneOtp(hiddenInputField.text);
            IncorrectCode.SetActive(!verified);

            if (verified)
            {
                successCodeField();
            }
            else
            {
                failedCodeField();
            }
        }
    }


    private void ResendOTP()
    {
        clearCodeField();

        if (timer <= 0)
        {
            timer = resendCooldown;
            if (isEmailAuth)
            {
                SendEmailOtp();
            }
            else
            {
                SendPhoneOtp();
            }
        }
    }
    private async void SendEmailOtp()
    {
        string email = emailAddress.text;
        if (string.IsNullOrEmpty(email))
        {
            Debug.Log("Email cannot be empty.");
            return;
        }

        var (success, token, error) = await OktoProviderSDK.Instance.SendEmailOtpAsync(email);
        if (success)
        {
            timer = 30;
            emailToken = token;
            CloseAllScreens();
            clearCodeField();
            defaultObjects.SetActive(false);
            verificationText.text = MaskEmail(email);
            logoImage.sprite = logoEmail;
            clearCodeField();
            confirmationScreen.SetActive(true);
        }
        else
        {
            Debug.LogError($"Error: {error?.Message ?? "Failed to send email OTP"}");
        }
    }

    public async Task<bool> VerifyEmailOtp(string otp)
    {
        string email = emailAddress.text;

        var (success, authToken, error) = await OktoProviderSDK.Instance.VerifyEmailOtpAsync(email, otp, emailToken);
        if (success)
        {
            Debug.Log(authToken);
            CloseAllScreens();
            clearCodeField();
            phoneScreen.SetActive(true);
            defaultObjects.SetActive(true);
            onboardingScreen.SetActive(false);
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            return true;
        }
        else
        {
            return false;
        }
    }

    private async void SendPhoneOtp()
    {
        string number = phoneNumber.text;
        string countryCode = "IN";

        var (success, token, error) = await OktoProviderSDK.Instance.SendPhoneOtpAsync(number, countryCode);
        if (success)
        {
            timer = 30;
            phoneToken = token;
            CloseAllScreens();
            clearCodeField();
            defaultObjects.SetActive(false);
            verificationText.text = MaskPhoneNumber(number);
            logoImage.sprite = logoPhone;
            clearCodeField();
            confirmationScreen.SetActive(true);
        }
    }

    public async Task<bool> VerifyPhoneOtp(string otp)
    {
        string number = phoneNumber.text;
        string countryCode = "IN";

        var (success, authToken, error) = await OktoProviderSDK.Instance.VerifyPhoneOtpAsync(number, countryCode, otp, phoneToken);
        if (success)
        {
            Debug.Log(authToken);
            CloseAllScreens();
            phoneScreen.SetActive(true);
            defaultObjects.SetActive(true);
            clearCodeField();
            onboardingScreen.SetActive(false);
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            return true;
        }
        else
        {
            return false;
        }
    }
    private string MaskPhoneNumber(string phoneNumber)
    {
        if (phoneNumber.Length >= 10)
        {
            return $"{phoneNumber.Substring(0, 2)}****{phoneNumber.Substring(8)}";
        }
        return phoneNumber;
    }

    private string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            return email;

        string[] parts = email.Split('@');
        string localPart = parts[0];
        string domain = parts[1];

        if (localPart.Length > 2)
        {
            localPart = $"{localPart[0]}****{localPart[^1]}";
        }

        return $"{localPart}@{domain}";
    }

}
