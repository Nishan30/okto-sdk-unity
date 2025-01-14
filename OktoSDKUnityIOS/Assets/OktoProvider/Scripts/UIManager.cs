using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OktoProvider;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

public class UIManager : MonoBehaviour
{
    //Buttons 
    [Header("Buttons")]
    [SerializeField] private Button authenticateButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button getPortfolio;
    [SerializeField] private Button getUserDetail;
    [SerializeField] private Button getSupportedTokens;
    [SerializeField] private Button getSupportedNetworks;
    [SerializeField] private Button orderHistory;
    [SerializeField] private Button getWallet;
    [SerializeField] private Button getNFTOrder;
    [SerializeField] private Button createWallet;
    [SerializeField] private Button showModel;
    [SerializeField] private Button onBoardingNative;

    //TokenTransfer
    [Header("Token Transfer UI")]
    [SerializeField] private TMP_InputField tokenAddress;
    [SerializeField] private TMP_InputField receipent_address;
    [SerializeField] private TMP_InputField amount;
    [SerializeField] private TMP_InputField networkName;
    [SerializeField] private Button transferTokenButton;

    [Header("NFT Transfer UI")]
    [SerializeField] private TMP_InputField collectionAddress;
    [SerializeField] private TMP_InputField recipientAddress;
    [SerializeField] private TMP_InputField collectionName;
    [SerializeField] private TMP_InputField nftQuantity;
    [SerializeField] private TMP_InputField nftAddress;
    [SerializeField] private TMP_InputField networkNameNFT;
    [SerializeField] private Button transferNFTButton;

    [Header("Raw Transaction UI SOL")]
    [SerializeField] private TMP_InputField instructions;
    [SerializeField] private TMP_InputField signer_address;
    [SerializeField] private TMP_InputField network_name_sol;
    [SerializeField] private Button solTransactionButton;

    //Raw Transaction UI EVM
    [Header("Raw Transaction UI EVM")]
    [SerializeField] private TMP_InputField tokenAddress_from;
    [SerializeField] private TMP_InputField tokenAddress_to;
    [SerializeField] private TMP_InputField evm_data;
    [SerializeField] private TMP_InputField evm_value;
    [SerializeField] private TMP_InputField network_name_evm;
    [SerializeField] private Button evmTransactionButton;

    [Header("Raw Transaction UI APTOS")]
    [SerializeField] private TMP_InputField aptosFunction;
    [SerializeField] private TMP_InputField aptosTypeArguments;
    [SerializeField] private TMP_InputField aptosFunctionArguments;
    [SerializeField] private TMP_InputField network_name_aptos;
    [SerializeField] private Button aptosTransactionButton;

    [Header("ReadContract")]
    [SerializeField] private TMP_InputField readData;
    [SerializeField] private TMP_InputField network_Name;
    [SerializeField] private Button readContractButton;

    //DisplayUI
    [Header("Display UI Elements")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private GameObject scrollObject;
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private TMP_Text loginText;
    [SerializeField] private DisplayObject displayObject;
    [SerializeField] private Transform objectHolder;

    // Otp UI
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField phoneInputField;
    [SerializeField] private TMP_InputField countryCodeInputField;
    [SerializeField] private TMP_InputField otpInputFieldEmail;
    [SerializeField] private TMP_InputField otpInputFieldPhone;

    //ApiKeys and BuildStage
    [SerializeField] private TMP_InputField apiKeyText;
    [SerializeField] private TMP_Dropdown buildStage;
    [SerializeField] private GameObject OpeningPanel;

    [SerializeField] private Button SendEmailOtpButton;
    [SerializeField] private Button SendPhoneOtpButton;

    [SerializeField] private OnboardingManager onboardingWidget;
    [SerializeField] private OktoWebViewWidget webWidget;
    [SerializeField] private GameObject onboardingUI;

    [SerializeField] private string webClientId;

    private string emailToken;
    private string phoneToken;

    private List<DisplayObject> displayObjects = new List<DisplayObject>();
    public OktoProviderSDK oktoProvider;
    private AuthDetails authenticationData;
    private Credentials credentials;

    private void setup()
    {
        int selectedIndex = buildStage.value;
        DataManager.Instance.buildStage = buildStage.options[selectedIndex].text;
        oktoProvider = new OktoProviderSDK(apiKeyText.text, buildStage.options[selectedIndex].text, webClientId);
        DataManager.Instance.apiKey = apiKeyText.text;
        Debug.Log("apiKey" + apiKeyText.text);
        Debug.Log("OktoProviderSDK initialized.");
        OpeningPanel.SetActive(false);
        onboardingWidget.loggedIn();
    }

    private void OnEnable()
    {
        authenticateButton.onClick.AddListener(LoginPressedAsync);
        logoutButton.onClick.AddListener(logoutButtonPressed);
        getPortfolio.onClick.AddListener(OnGetPortfolioClicked);
        getUserDetail.onClick.AddListener(OnGetUserDetailClicked);
        getSupportedTokens.onClick.AddListener(OnGetSupportedTokensClicked);
        orderHistory.onClick.AddListener(OnOrderHistoryClicked);
        getWallet.onClick.AddListener(OnGetWalletClicked);
        getNFTOrder.onClick.AddListener(OnGetNFTOrderClicked);
        createWallet.onClick.AddListener(OnCreateWalletClicked);
        //showModel.onClick.AddListener(OnShowModelClicked);
        getSupportedNetworks.onClick.AddListener(OnSupportedNetworksClicked);
        transferTokenButton.onClick.AddListener(OnTransferTokenClicked);
        solTransactionButton.onClick.AddListener(OnSOLRawTransactionClicked);
        evmTransactionButton.onClick.AddListener(OnEVMRawTransactionClicked);
        aptosTransactionButton.onClick.AddListener(OnAptosRawTransactionClicked);
        readContractButton.onClick.AddListener(OnReadContractClicked);
        transferNFTButton.onClick.AddListener(OnTransferNFTClicked);
        SendEmailOtpButton.onClick.AddListener(SendEmailOtp);
        SendPhoneOtpButton.onClick.AddListener(SendPhoneOtp);
        onBoardingNative.onClick.AddListener(openOnboarding);
    }

    private void OnDisable()
    {
        logoutButton.onClick.RemoveListener(logoutButtonPressed);
        getPortfolio.onClick.RemoveListener(OnGetPortfolioClicked);
        getUserDetail.onClick.RemoveListener(OnGetUserDetailClicked);
        getSupportedTokens.onClick.RemoveListener(OnGetSupportedTokensClicked);
        orderHistory.onClick.RemoveListener(OnOrderHistoryClicked);
        getWallet.onClick.RemoveListener(OnGetWalletClicked);
        getNFTOrder.onClick.RemoveListener(OnGetNFTOrderClicked);
        createWallet.onClick.RemoveListener(OnCreateWalletClicked);
        //showModel.onClick.RemoveListener(OnShowModelClicked);
        getSupportedNetworks.onClick.RemoveListener(OnTransferTokenClicked);
        transferTokenButton.onClick.RemoveListener(OnTransferTokenClicked);
        solTransactionButton.onClick.RemoveListener(OnSOLRawTransactionClicked);
        evmTransactionButton.onClick.RemoveListener(OnEVMRawTransactionClicked);
        aptosTransactionButton.onClick.RemoveListener(OnAptosRawTransactionClicked);
        readContractButton.onClick.RemoveListener(OnReadContractClicked);
        transferNFTButton.onClick.RemoveListener(OnTransferNFTClicked);
        SendEmailOtpButton.onClick.RemoveAllListeners();
        SendPhoneOtpButton.onClick.RemoveAllListeners();
        onBoardingNative.onClick.RemoveListener(openOnboarding);
    }


    private async void LoginPressedAsync()
    {
        Debug.Log("Attempting login with OktoProvider...");

        Exception error = null;

        try
        {
            (authenticationData, error) = await oktoProvider.LoginGoogle();

            if (error != null)
            {
                Debug.LogError($"Login failed with error: {error.Message}");
            }
            else
            {
                Debug.Log("Login successful!");
            }
            Debug.Log("loginDone" + authenticationData);
            displayOutput("AuthTokens" + authenticationData.authToken.ToString());
            DataManager.Instance.AuthToken = authenticationData.authToken;
            webWidget.loggedIn();
            if (authenticationData != null)
            {
                Debug.Log("Login successful.");
                loginText.text = "Logged In";
                try
                {
                    var wallet = await oktoProvider.GetWallets();
                    if (wallet.wallets.Count > 0)
                    {
                        createWallet.gameObject.SetActive(false);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Please create wallet.");
                }

            }
            else
            {
                Debug.LogError("Login failed: Invalid API Key.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error during login: {ex.Message}");
        }
    }


    private void logoutButtonPressed()
    {
        oktoProvider.Logout();
        Debug.Log("Log out successful.");
        loginText.text = "Login";
        createWallet.gameObject.SetActive(true);
    }

    public async void Authenticate(string id)
    {
        try
        {
            Debug.Log("login" + oktoProvider);
            Exception error = null;
            (authenticationData, error) = await oktoProvider.AuthenticateAsync(id);
            Debug.Log("loginDone" + authenticationData);
            displayOutput("AuthTokens" + authenticationData.authToken.ToString());
            webWidget.loggedIn();
            if (authenticationData != null)
            {
                Debug.Log("Login successful.");
                loginText.text = "Logged In";
                try
                {
                    var wallet = await oktoProvider.GetWallets();
                    if (wallet.wallets.Count > 0)
                    {
                        createWallet.gameObject.SetActive(false);
                    }
                }
                catch(Exception e)
                {
                    Debug.Log("Please create wallet.");
                }

            }
            else
            {
                Debug.LogError("Login failed: Invalid API Key.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Login failed: " + e.Message);
        }
    }

    public async void authenticationCompleted(string token)
    {
        webWidget.loggedIn();
        Debug.Log("loginDone");
        displayOutput("AuthTokens" + token);
        DataManager.Instance.AuthToken = token;
        loginText.text = "Logged In";
        var wallet = await oktoProvider.GetWallets();
        if (wallet.wallets.Count > 0)
        {
            createWallet.gameObject.SetActive(false);
        }
    }

    private async void OnGetPortfolioClicked()
    {
        try
        {
            var portfolio = await oktoProvider.GetPortfolio();
            foreach (Portfolio token in portfolio.tokens)
            {
                DisplayObject dObject = Instantiate(displayObject, objectHolder);
                displayObjects.Add(dObject);
                dObject.setup(token.token_name, token.token_address, token.quantity);

            }
            displayTokens();
            Debug.Log("Portfolio retrieved: " + portfolio);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to retrieve portfolio: " + e.Message);
        }
    }

    private async void OnSupportedNetworksClicked()
    {
        try
        {
            var networks = await oktoProvider.GetSupportedNetworks();
            foreach (TokenNetwork token in networks.network)
            {
                DisplayObject dObject = Instantiate(displayObject, objectHolder);
                displayObjects.Add(dObject);
                dObject.setup(token.network_name, token.chain_id, token.logo);

            }
            displayTokens();
            Debug.Log("Portfolio retrieved: " + networks);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to retrieve portfolio: " + e.Message);
        }
    }

    private async void OnTransferTokenClicked()
    {
        TransferTokens tokenData = new TransferTokens();
        tokenData.network_name = networkName.text;
        tokenData.quantity = amount.text;
        tokenData.recipient_address = receipent_address.text;
        tokenData.token_address = tokenAddress.text;
        try
        {
            var tokenTransferData = await oktoProvider.TransferTokens_(tokenData);
            displayOutput("Order Id: " + tokenTransferData.orderId);
            Debug.Log("Order Id: " + tokenTransferData.orderId);
        }
        catch (Exception e)
        {
            displayOutput("Could not send order.");
        }
    }
    private async void OnTransferNFTClicked()
    {
        TransferNft nftData = new TransferNft();

        nftData.network_name = networkNameNFT.text; 
        nftData.opteration_type = "NFT_TRANSFER";  
        nftData.collection_address = collectionAddress.text;
        nftData.collection_name = collectionName.text;  
        nftData.quantity = nftQuantity.text;
        nftData.recipient_address = recipientAddress.text;
        nftData.nft_address = nftAddress.text;

        try
        {
            var nftTransferData = await oktoProvider.transferNft(nftData);
            Debug.Log("Order Id: " + nftTransferData.order_id);
            displayOutput("Order Id: " + nftTransferData.order_id);
        }
        catch (Exception e)
        {
            displayOutput("Could not send order.");
        }
    }


    private async void OnSOLRawTransactionClicked()
    {
        ExecuteRawTransaction tokenData = new ExecuteRawTransaction();
        tokenData.network_name = network_name_sol.text;
        string instructionsJson = instructions.text;
        var instructionsVal = JsonConvert.DeserializeObject<List<Instruction>>(instructionsJson);
        SOLTransaction sol = new SOLTransaction();
        sol.instructions = instructionsVal;
        var signerList = new List<String>();
        signerList.Add(signer_address.text);
        sol.signers = signerList;
        tokenData.transaction = sol;
        try
        {
            var transactionData = await oktoProvider.executeRawTransactionSol(tokenData);
            Debug.Log("Job Id: " + transactionData.orderId);
            displayOutput("Job Id: " + transactionData.orderId);
        }
        catch (Exception e)
        {
            displayOutput("Could not send transaction.");
        }
    }

    private async void OnEVMRawTransactionClicked()
    {
        ExecuteRawTransaction tokenData = new ExecuteRawTransaction();
        tokenData.network_name = network_name_evm.text;
        EVMTransaction evm = new EVMTransaction();
        evm.from = tokenAddress_from.text;
        evm.to = tokenAddress_to.text;
        evm.data = evm_data.text;
        evm.value = evm_value.text;
        tokenData.transaction = evm;
        try
        {
            var transactionData = await oktoProvider.executeRawTransactionPol(tokenData);
            Debug.Log("Job Id: " + transactionData.jobId);
            displayOutput("Job Id: " + transactionData.jobId);
        }
        catch (Exception e)
        {
            displayOutput("Could not send transaction.");
        }
    }

    private async void OnAptosRawTransactionClicked()
    {
        ExecuteRawTransaction tokenData = new ExecuteRawTransaction();
        tokenData.network_name = "APTOS"; 

        AptosTransaction aptos = new AptosTransaction();
        aptos.transactions = new List<AptosTransactionItem>
        {
            new AptosTransactionItem
            {
                function = aptosFunction.text, 
                typeArguments = aptosTypeArguments.text.Split(','), 
                functionArguments = aptosFunctionArguments.text.Split(',') 
            }
        };

        tokenData.transaction = aptos;

        try
        {
            var transactionData = await oktoProvider.executeRawTransactionSol(tokenData);
            Debug.Log("Order Id: " + transactionData.orderId);
            displayOutput("Order Id: " + transactionData.orderId);
        }
        catch (Exception e)
        {
            displayOutput("Could not send transaction.");
        }
    }

    private async void OnReadContractClicked()
    {
        string networkNameInput = network_Name.text; 
        string dataInput = readData.text;

        try
        {
            ContractRequestData requestData = new ContractRequestData();
            ContractData data = JsonConvert.DeserializeObject<ContractData>(dataInput);
            requestData.network_name = networkNameInput;
            requestData.data = data;

            Debug.Log("Request Data: " + JsonConvert.SerializeObject(requestData, Formatting.Indented));

            var transactionData = await oktoProvider.readContractData(requestData);
            Debug.Log("Data: " + transactionData[0]);
            displayOutput("Data: " + transactionData[0]);
        }
        catch (Exception e)
        {
            displayOutput("Could not read contract: " + e.Message);
        }
    }

    private async void OnGetUserDetailClicked()
    {
        try
        {
            var userDetail = await oktoProvider.GetUserDetails();
            displayOutput("User Id: " + userDetail.user_id + " Email" + userDetail.email);
            Debug.Log("User Detail: " + userDetail);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to get user detail: " + e.Message);
        }
    }

    private async void OnGetSupportedTokensClicked()
    {
        try
        {
            var tokens = await oktoProvider.GetSupportedTokens();
            foreach (Token token in tokens.tokens)
            {
                DisplayObject dObject = Instantiate(displayObject,objectHolder);
                displayObjects.Add(dObject);
                dObject.setup(token.token_address, token.token_name, token.network_name);
                
            }
            displayTokens();
            Debug.Log("Supported Tokens: " + tokens);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to get supported tokens: " + e.Message);
        }
    }

    private async void OnOrderHistoryClicked()
    {
        try
        {
            OrderQuery query = new OrderQuery();
            var orders = await oktoProvider.OrderHistory(query);
            displayOutput("Number of orders " + orders.total);
            Debug.Log("Number of orders " + orders.total);
        }
        catch (Exception e)
        {
            displayOutput("No Order History");
        }
    }

    private async void OnGetWalletClicked()
    {
        try
        {
            var wallet = await oktoProvider.GetWallets();
            if(wallet.wallets.Count == 0)
            {
                displayOutput("You need to create wallet first.");
                return;
            }
            foreach (Wallet token in wallet.wallets)
            {
                DisplayObject dObject = Instantiate(displayObject, objectHolder);
                displayObjects.Add(dObject);
                dObject.setup(token.address, token.network_name, "");

            }
            displayTokens();
            Debug.Log("Wallet: " + wallet);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to get wallet: " + e.Message);
        }
    }

    private async void OnGetNFTOrderClicked()
    {
        try
        {
            NftOrderDetailsQuery query = new NftOrderDetailsQuery();
            var nftOrder = await oktoProvider.GetNftOrderDetails(query);
            displayOutput("Number of NFT Orders: " + nftOrder.count.ToString());
            Debug.Log("NFT Order: " + nftOrder);
        }
        catch (Exception e)
        {
            displayOutput("No NFT Orders");
        }
    }

    private async void OnCreateWalletClicked()
    {
        try
        {
            var walletCreationResult = await oktoProvider.CreateWallet();
            displayOutput("Wallet created");
            Debug.Log("Wallet created: " + walletCreationResult);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to create wallet: " + e.Message);
        }
    }

    private async void SendEmailOtp()
    {
        string email = emailInputField.text;
        if (string.IsNullOrEmpty(email))
        {
            displayOutput("Email cannot be empty.");
            return;
        }

        var (success, token, error) = await oktoProvider.SendEmailOtpAsync(email);
        if (success)
        {
            emailToken = token;
            displayOutput("Email OTP sent successfully!");
            otpInputFieldEmail.gameObject.SetActive(true);
            SendEmailOtpButton.onClick.RemoveListener(SendEmailOtp);
            SendEmailOtpButton.onClick.AddListener(VerifyEmailOtp);
        }
        else
        {
            displayOutput($"Error: {error?.Message ?? "Failed to send email OTP"}");
        }
    }

    private async void VerifyEmailOtp()
    {
        string email = emailInputField.text;
        string otp = otpInputFieldEmail.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(emailToken))
        {
            displayOutput("All fields must be filled in for verification.");
            return;
        }

        var (success, authToken, error) = await oktoProvider.VerifyEmailOtpAsync(email, otp, emailToken);
        if (success)
        {
            displayOutput("Email OTP verified successfully!");
            authenticationCompleted(authToken);
        }
        else
        {
            displayOutput($"Error: {error?.Message ?? "Failed to verify email OTP"}");
        }
    }

    private async void SendPhoneOtp()
    {
        string phoneNumber = phoneInputField.text;
        string countryCode = countryCodeInputField.text;

        if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(countryCode))
        {
            displayOutput("Phone number and country code cannot be empty.");
            return;
        }

        var (success, token, error) = await oktoProvider.SendPhoneOtpAsync(phoneNumber, countryCode);
        if (success)
        {
            phoneToken = token;
            displayOutput("Phone OTP sent successfully!");
            otpInputFieldPhone.gameObject.SetActive(true);
            SendPhoneOtpButton.onClick.RemoveListener(SendPhoneOtp);
            SendPhoneOtpButton.onClick.AddListener(VerifyPhoneOtp);
        }
        else
        {
            displayOutput($"Error: {error?.Message ?? "Failed to send phone OTP"}");
        }
    }

    private async void VerifyPhoneOtp()
    {
        string phoneNumber = phoneInputField.text;
        string countryCode = countryCodeInputField.text;
        string otp = otpInputFieldPhone.text;

        if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(countryCode) || string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(phoneToken))
        {
            displayOutput("All fields must be filled in for verification.");
            return;
        }

        var (success, authToken, error) = await oktoProvider.VerifyPhoneOtpAsync(phoneNumber, countryCode, otp, phoneToken);
        if (success)
        {
             displayOutput("Phone OTP verified successfully!");
            authenticationCompleted(authToken);
        }
        else
        {
            displayOutput($"Error: {error?.Message ?? "Failed to verify phone OTP"}");
        }
    }


    public void displayOutput(string text)
    {
        scrollObject.gameObject.SetActive(false);
        displayPanel.gameObject.SetActive(true);
        displayText.gameObject.SetActive(true);
        displayText.text = text;
    }

    public void clearDisplayObjects()
    {
        foreach(DisplayObject dObject in displayObjects)
        {
            Destroy(dObject.gameObject);
        }
        displayObjects.Clear();
    }

    public void displayTokens()
    {
        displayText.gameObject.SetActive(false);
        displayPanel.gameObject.SetActive(true);
        scrollObject.gameObject.SetActive(true);
    }

    private void openOnboarding()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        onboardingUI.SetActive(true);
    }
}
