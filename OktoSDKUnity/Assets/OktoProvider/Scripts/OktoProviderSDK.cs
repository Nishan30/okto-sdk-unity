using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;
#if ENABLE_GOOGLE_PLAY_GAMES
    using GooglePlayGames.BasicApi;
    using GooglePlayGames;
#endif


namespace OktoProvider
{
    public class OktoProviderSDK : MonoBehaviour
    {
        private static OktoProviderSDK _instance;
        public static OktoProviderSDK Instance => _instance ?? throw new Exception("SDK not initialized. Ensure OktoProviderSDK is in the scene.");
        private static HttpClient httpClient;
        private AuthDetails authDetails;
        private string baseUrl;
        private int JOB_MAX_RETRY = 50;
        private int JOB_RETRY_INTERVAL = 2;
        private Credentials credentials;
        public static event Action OnSDKInitialized;

        public string AuthToken { get; set; }
        public string RefreshToken { get; set; }
        public string DeviceToken { get; set; }
        public string apiKey { get; set; }
        public string buildStage { get; set; }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSDK();
            }
            else
            {
                Destroy(gameObject); 
            }
        }

        private void InitializeSDK()
        {
            InitializePlayGamesLogin();
            credentials = Resources.Load<Credentials>("Credentials");
            apiKey = credentials.apiKey;
            baseUrl = GetBaseUrl("");
            buildStage = "SANDBOX";
            apiKey = apiKey;
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(baseUrl);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            OnSDKInitialized?.Invoke();
        }

        void InitializePlayGamesLogin()
        {
#if ENABLE_GOOGLE_PLAY_GAMES
                var config = new PlayGamesClientConfiguration.Builder()
                    .RequestIdToken()
                    .RequestEmail()
                    .Build();

                PlayGamesPlatform.InitializeInstance(config);
                PlayGamesPlatform.DebugLogEnabled = true;
                PlayGamesPlatform.Activate();
#else
            Debug.LogWarning("Google Sign-In is not enabled. Please add ENABLE_GOOGLE_PLAY_GAMES to scripting define symbols.");

#endif
        }
        public async Task<(AuthDetails result, Exception error)> LoginGoogle()
        {
#if ENABLE_GOOGLE_PLAY_GAMES
                var tcs = new TaskCompletionSource<(string idToken, Exception error)>();

                // Authenticate with Play Games
                Social.localUser.Authenticate(success =>
                {
                    if (success)
                    {
                        string idToken = ((PlayGamesLocalUser)Social.localUser).GetIdToken();
                        Debug.Log($"Google Login successful. IdToken: {idToken}");
                        tcs.SetResult((idToken, null));
                    }
                    else
                    {
                        Debug.LogWarning("Google Login failed.");
                        tcs.SetResult((null, new Exception("Google authentication failed.")));
                    }
                });

                // Wait for Play Games authentication result
                var (idToken, authError) = await tcs.Task;

                if (authError != null || string.IsNullOrEmpty(idToken))
                {
                    return (null, authError ?? new Exception("IdToken is null or empty."));
                }

                // Pass IdToken to backend authentication
                return await AuthenticateAsync(idToken);
#else
            Debug.LogError("Google Sign-In is not enabled. Please add ENABLE_GOOGLE_PLAY_GAMES to scripting define symbols.");
            return (null, null);
#endif
        }



        [Serializable]
        private class TokenResponse
        {
            public string id_token;
        }

        private string GetBaseUrl(string buildType)
        {
            if (buildType == "PRODUCTION")
            {
                return "https://apigw.okto.tech";
            }
            else if (buildType == "STAGING")
            {
                return "https://3p-bff.oktostage.com";
            }
            else
            {
                return "https://sandbox-api.okto.tech";
            }
        }

        public void Logout()
        {
            var newAuthDetails = new AuthDetails
            {
                authToken = "",
                refreshToken = "",
                deviceToken = ""
            };

            _ = UpdateAuthDetails(newAuthDetails);
        }

        private void SetAuthorizationHeader(string authToken)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }

        public AuthDetails GetAuthDetails()
        {
            if (AuthToken.Length > 0)
            {
                return new AuthDetails
                {
                    authToken = AuthToken ?? string.Empty,
                    refreshToken = RefreshToken ?? string.Empty,
                    deviceToken = DeviceToken ?? string.Empty
                };
            }
            return null;
        }

        public async Task UpdateAuthDetails(AuthDetails newAuthDetails)
        {
            AuthToken = newAuthDetails.authToken;
            RefreshToken = newAuthDetails.refreshToken;
            DeviceToken = newAuthDetails.deviceToken;
            authDetails = newAuthDetails;
            //SetAuthorizationHeader(authDetails.authToken);
            //await SaveAuthDetailsToLocalStorage(authDetails);
        }

        private async Task<AuthDetails> LoadAuthDetailsFromLocalStorage()
        {
            string storedAuthDetails = "";
            return JsonConvert.DeserializeObject<AuthDetails>(storedAuthDetails);
        }

        private async Task SaveAuthDetailsToLocalStorage(AuthDetails details)
        {
            string authDetailsJson = JsonConvert.SerializeObject(details);
        }


        public async Task<(AuthDetails result, Exception error)> AuthenticateAsync(string idToken)
        {
            if (httpClient == null)
            {
                return (null, new Exception("SDK is not initialized"));
            }

            try
            {
                var requestBody = new
                {
                    id_token = idToken
                };
                var jsonString = JsonConvert.SerializeObject(requestBody);
                Debug.Log("Serialized JSON: " + jsonString);

                var jsonContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                Debug.Log("jsonContent: " + jsonContent);

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36");
                httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
                Debug.Log("login" + idToken);
                Debug.Log("login2here");
                Debug.Log("login" + jsonContent);
                var response = await httpClient.PostAsync($"{baseUrl}/api/v2/authenticate", jsonContent);
                Debug.Log("login" +  response);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log("Response Content: " + responseContent);

                    var responseData = JsonConvert.DeserializeObject<AuthResponse>(responseContent);
                    Debug.Log(responseData.status);
                    if (responseData?.status == "success" && responseData?.data?.auth_token != null)
                    {
                        var authDetailsNew = new AuthDetails
                        {
                            authToken = responseData.data.auth_token,
                            refreshToken = responseData.data.refresh_auth_token,
                            deviceToken = responseData.data.device_token
                        };
                        UpdateAuthDetails(authDetailsNew);
                        return (authDetailsNew, null);
                    }
                    return (null, new Exception("Server responded with an error: " + responseContent));
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return (null, new Exception("Server responded with an error: " + errorContent));
                }
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        }

        public async Task<AuthDetails> RefreshToken_()
        {
            if (authDetails != null)
            {
                try
                {

                    var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/refresh_token");
                    request.Headers.Add("x-refresh-authorization", $"Bearer {authDetails.refreshToken}");
                    request.Headers.Add("x-device-token", authDetails.deviceToken);

                    var response = await httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonConvert.DeserializeObject<AuthResponse>(content);

                    var newAuthDetails = new AuthDetails
                    {
                        authToken = authResponse.data.auth_token,
                        refreshToken = authResponse.data.refresh_auth_token,
                        deviceToken = authResponse.data.device_token
                    };

                    await UpdateAuthDetails(newAuthDetails);
                    return newAuthDetails;
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to refresh token: " + ex.Message);
                }
            }
            return null;
        }

        public async Task<ApiResponse<T>> MakeGetRequest<T>(string endpoint, string queryUrl = null)
        {
            var url = queryUrl != null ? $"{endpoint}?{queryUrl}" : endpoint;

            try
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + AuthToken);
                var response = await httpClient.GetAsync($"{baseUrl}/api"+url);
                Debug.Log(response);
                Debug.Log($"{baseUrl}/api" + url);
                Debug.Log(AuthToken);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                Debug.Log(content);
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(content);
                Debug.Log(apiResponse.data);
                if (apiResponse.status == "success")
                {
                    return apiResponse;
                }

                throw new Exception("Server responded with an error.");
            }
            catch (Exception ex)
            {
                throw new Exception("Request failed: " + ex.Message);
            }
        }

        public async Task<ApiResponse<T>> MakePostRequest<T>(string endpoint, object data = null)
        {
            try
            {
                var jsonContent = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + AuthToken);
                Debug.Log(AuthToken);
                Debug.Log(jsonContent);
                var response = await httpClient.PostAsync($"{baseUrl}/api" + endpoint, content);
               

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log(responseContent);
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent);
                Debug.Log(apiResponse);
                if (apiResponse.status == "success")
                {
                    return apiResponse;
                }

                throw new Exception("Server responded with an error.");
            }
            catch (Exception ex)
            {
                throw new Exception("Request failed: " + ex.Message);
            }
        }

        public async Task<PortfolioData> GetPortfolio()
        {
            ApiResponse<PortfolioData> response = await MakeGetRequest<PortfolioData>("/v1/portfolio");
            return response.data;
        }
        public async Task<TokensDataNetworks> GetSupportedNetworks()
        {
            ApiResponse<TokensDataNetworks> response = await MakeGetRequest<TokensDataNetworks>("/v1/supported/networks");
            return response.data;
        }
        public async Task<TokensData> GetSupportedTokens()
        {
            ApiResponse<TokensData> response = await MakeGetRequest<TokensData>("/v1/supported/tokens");
            return response.data;
        }

        public async Task<User> GetUserDetails()
        {
            ApiResponse<User> response = await MakeGetRequest<User>("/v1/user_from_token");
            return response.data;
        }

        public async Task<OrderData> OrderHistory(OrderQuery query)
        {
            string queryString = GetQueryString(query);
            ApiResponse<OrderData> response = await MakeGetRequest<OrderData>($"/v1/orders");
            return response.data;
        }


        public async Task<WalletData> GetWallets()
        {
            ApiResponse<WalletData> response = await MakeGetRequest<WalletData>("/v1/wallet");
            return response.data;
        }
        public async Task<NftOrderDetailsData> GetNftOrderDetails(NftOrderDetailsQuery query)
        {
            string queryString = GetQueryString(query);
            ApiResponse<NftOrderDetailsData> response = await MakeGetRequest<NftOrderDetailsData>($"/v1/nft/order_details?{queryString}");
            return response.data;
        }

        public async Task<RawTransactionStatusData> GetRawTransactionStatus(RawTransactionStatusQuery query)
        {
            string queryString = GetQueryString(query);
            ApiResponse<RawTransactionStatusData> response = await MakeGetRequest<RawTransactionStatusData>($"/v1/rawtransaction/status?{queryString}");
            return response.data;
        }
        public async Task<WalletData> CreateWallet()
        {
            ApiResponse<WalletData> response = await MakePostRequest<WalletData>("/v1/wallet");
            return response.data;
        }
        public async Task<TransferTokensData> TransferTokens_(TransferTokens data)
        {
            ApiResponse<TransferTokensData> response = await MakePostRequest<TransferTokensData>("/v1/transfer/tokens/execute", data);
            return response.data;
        }
        public async Task<Order> TransferTokensWithJobStatus(TransferTokens data)
        {
            var transferResponse = await TransferTokens_(data);
            string orderId = transferResponse.orderId;

            return await WaitForJobCompletion(orderId, async (id) =>
            {
                var orderData = await OrderHistory(new OrderQuery { order_id = id });
                return orderData.jobs.FirstOrDefault(job => job.order_id == id && (job.status == "success" || job.status == "failed"));
            });
        }

        public async Task<ExecuteRawTransactionDataPol> executeRawTransactionPol(ExecuteRawTransaction data)
        {
            ApiResponse<ExecuteRawTransactionDataPol> response = await MakePostRequest<ExecuteRawTransactionDataPol>($"/v1/rawtransaction/execute?network_name={data.network_name}", data);
            return response.data;
        }

        public async Task<ExecuteRawTransactionDataSol> executeRawTransactionSol(ExecuteRawTransaction data)
        {
            ApiResponse<ExecuteRawTransactionDataSol> response = await MakePostRequest<ExecuteRawTransactionDataSol>($"/v1/rawtransaction/execute?network_name={data.network_name}", data);
            return response.data;
        }

        public async Task<RawTransactionStatus> ExecuteRawTransactionWithJobStatus(ExecuteRawTransaction data)
        {
            try
            {
                var jobId = await executeRawTransactionPol(data);
                Debug.Log($"Execute Raw transaction called with Job ID {jobId}");

                return await WaitForJobCompletion<RawTransactionStatus>(
                    jobId.jobId,
                    async (string orderId) =>
                    {
                        RawTransactionStatusQuery query = new RawTransactionStatusQuery();
                        query.order_id = orderId;
                        var orderData = await GetRawTransactionStatus(query);
                        var order = orderData.jobs.Find(item => item.order_id == orderId);

                        if (order != null &&
                            (order.status == "success" || order.status == "failed"))
                        {
                            return order;
                        }

                        throw new Exception($"Order with ID {orderId} not found or not completed.");
                    }
                );
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                throw;
            }
        }

        private async Task<T> WaitForJobCompletion<T>(string orderId, Func<string, Task<T>> findJobCallback)
        {
            for (int retryCount = 0; retryCount < JOB_MAX_RETRY; retryCount++)
            {
                try
                {
                    return await findJobCallback(orderId);
                }
                catch
                {
                    // Ignore exception to allow retry
                }
                await Task.Delay(JOB_RETRY_INTERVAL);
            }
            throw new Exception($"Order ID {orderId} not found or not completed.");
        }
        public string GetQueryString(object query)
        {
            var queryParams = new List<string>();

            // Use reflection to get properties and their values
            foreach (var property in query.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = property.GetValue(query)?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    queryParams.Add($"{property.Name}={value}");
                }
            }

            return string.Join("&", queryParams);
        } 

        public async Task<TransferNftData> transferNft(TransferNft data)
        {
            ApiResponse<TransferNftData> response = await MakePostRequest<TransferNftData>("/v1/nft/transfer", data);
            return response.data;
        }

        public async Task<(bool success, string token, Exception error)> SendEmailOtpAsync(string email)
        {
            var apiUrl = $"{baseUrl}/api/v1/authenticate/email";
            var requestBody = new { email = email };

            try
            {
                var response = await MakeApiCallAsync(apiUrl, requestBody);
                Debug.Log(response);
                if (response.success && response.data.TryGetValue("token", out var token))
                {
                    return (true, token.ToString(), null);
                }
                return (false, null, new Exception("Failed to send email OTP"));
            }
            catch (Exception ex)
            {
                return (false, null, ex);
            }
        }

        public async Task<(bool success, string authToken, Exception error)> VerifyEmailOtpAsync(string email, string otp, string token)
        {
            var apiUrl = $"{baseUrl}/api/v1/authenticate/email/verify";
            var requestBody = new { email = email, otp = otp, token = token };

            try
            {
                var response = await MakeApiCallAsync(apiUrl, requestBody);
                if (response.success && response.data.TryGetValue("auth_token", out var authToken))
                {
                    Debug.Log(authToken.ToString());
                    return (true, authToken.ToString(), null);
                }
                return (false, null, new Exception("Failed to verify email OTP"));
            }
            catch (Exception ex)
            {
                return (false, null, ex);
            }
        }

        public async Task<(bool success, string token, Exception error)> SendPhoneOtpAsync(string phoneNumber, string countryShortName)
        {
            var apiUrl = $"{baseUrl}/api/v1/authenticate/phone";
            var requestBody = new { phone_number = phoneNumber, country_short_name = countryShortName };

            try
            {
                var response = await MakeApiCallAsync(apiUrl, requestBody);
                if (response.success && response.data.TryGetValue("token", out var token))
                {
                    return (true, token.ToString(), null);
                }
                return (false, null, new Exception("Failed to send phone OTP"));
            }
            catch (Exception ex)
            {
                return (false, null, ex);
            }
        }

        public async Task<(bool success, string authToken, Exception error)> VerifyPhoneOtpAsync(string phoneNumber, string countryShortName, string otp, string token)
        {
            var apiUrl = $"{baseUrl}/api/v1/authenticate/phone/verify";
            var requestBody = new { phone_number = phoneNumber, country_short_name = countryShortName, otp = otp, token = token };

            try
            {
                var response = await MakeApiCallAsync(apiUrl, requestBody);
                if (response.success && response.data.TryGetValue("auth_token", out var authToken))
                {
                    return (true, authToken.ToString(), null);
                }
                return (false, null, new Exception("Failed to verify phone OTP"));
            }
            catch (Exception ex)
            {
                return (false, null, ex);
            }
        }

        private async Task<(bool success, Dictionary<string, object> data, Exception error)> MakeApiCallAsync(string apiUrl, object requestBody)
        {
            try
            {
                var jsonString = JsonConvert.SerializeObject(requestBody);
                var jsonContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                // Set headers
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36");
                Debug.Log(apiUrl);
                Debug.Log(requestBody);

                var response = await httpClient.PostAsync(apiUrl, jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                Debug.Log(response);

                // Check response status
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Deserialize JSON response into a nested object
                        var responseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

                        // Check the "status" field
                        if (responseData != null &&
                            responseData.TryGetValue("status", out var status) &&
                            status.ToString() == "success")
                        {
                            // Check the "data" field
                            if (responseData.TryGetValue("data", out var data) &&
                                data is Newtonsoft.Json.Linq.JObject dataJObject)
                            {
                                // Convert "data" JObject to Dictionary<string, object>
                                var dataDict = dataJObject.ToObject<Dictionary<string, object>>();
                                return (true, dataDict, null);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return (false, null, new Exception("Error parsing API response: " + ex.Message));
                    }
                }

                return (false, null, new Exception("Error in API response: " + responseContent));
            }
            catch (Exception ex)
            {
                return (false, null, ex);
            }
        }


    }
    public class Order
    {
        public string order_id { get; set; }
        public string network_name { get; set; }
        public string order_type { get; set; }
        public string status { get; set; }
        public string transaction_hash { get; set; }
    }

    public class ApiResponse<T>
    {
        public T data { get; set; }
        public string status { get; set; }
    }

    [System.Serializable]
    public class AuthDetails
    {
        public string authToken { get; set; }
        public string refreshToken { get; set; }
        public string deviceToken { get; set; }
    }

    public class AuthResponse
    {
        public AuthResponseData data { get; set; }
        public string status { get; set; }
    }

    public class AuthResponseData
    {
        public string auth_token { get; set; }
        public string refresh_auth_token { get; set; }
        public string device_token { get; set; }
    }

    public class PortfolioData
    {
        public Portfolio[] tokens { get; set; }
        public decimal total { get; set; }
    }

    [System.Serializable]
    public class Portfolio
    {
        public string token_name { get; set; }
        public string token_image { get; set; }
        public string token_address { get; set; }
        public string network_name { get; set; }
        public string quantity { get; set; }
        public string amount_in_inr { get; set; }
    }

    [System.Serializable]
    public class TokensDataNetworks
    {
        public List<TokenNetwork> network { get; set; }
    }

    public class TokensData
    {
        public List<Token> tokens { get; set; }
    }

    [System.Serializable]
    public class TokenNetwork
    {
        public string network_name { get; set; }
        public string chain_id { get; set; }
        public string logo { get; set; }
    }

    [System.Serializable]
    public class Token
    {
        public string token_name { get; set; }
        public string token_address { get; set; }
        public string network_name { get; set; }

    }

    public class User
    {
        public string email { get; set; }
        public string user_id { get; set; }
        public string created_at { get; set; }
        public string freezed { get; set; }
        public string freeze_reason { get; set; }
    }

    public class TransferTokensData
    {
        public string orderId { get; set; }
    }

    public class NftOrderDetails
    {
        public string explorer_smart_contract_url { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string collection_id { get; set; }
        public string collection_name { get; set; }
        public string nft_token_id { get; set; }
        public string token_uri { get; set; }
        public string id { get; set; }
        public string image { get; set; }
        public string collection_address { get; set; }
        public string collection_image { get; set; }
        public string network_name { get; set; }
        public string network_id { get; set; }
        public string nft_name { get; set; }
    }

    public class TransferNftData
    {
        public string order_id { get; set; }
    }

    public class OrderQuery
    {
        public int offset { get; set; }
        public int limit { get; set; }
        public string order_id { get; set; }
        public string order_state { get; set; }
    }

    public class Wallet
    {
        public string network_name { get; set; }
        public string address { get; set; }
        public bool success { get; set; }
    }

    public class WalletData
    {
        public List<Wallet> wallets { get; set; }
    }

    public class RawTransactionStatusQuery
    {
        public string order_id { get; set; }
    }

    public class TransferTokens
    {
        public string network_name { get; set; }
        public string token_address { get; set; }
        public string quantity { get; set; }
        public string recipient_address { get; set; }
    }

    public class RawTransactionStatus
    {
        public string order_id { get; set; }
        public string network_name { get; set; }
        public string status { get; set; }
        public string transaction_hash { get; set; }
    }

    public class RawTransactionStatusData
    {
        public int total { get; set; }
        public List<RawTransactionStatus> jobs { get; set; }
    }

    public class ExecuteRawTransaction
    {
        public string network_name { get; set; }
        public object transaction { get; set; }
    }

    public class ExecuteRawTransactionDataPol
    {
        public string jobId { get; set; }
    }

    public class ExecuteRawTransactionDataSol
    {
        public string orderId { get; set; }
    }
    public class OrderData
    {
        public int total { get; set; }
        public List<Order> jobs { get; set; }
    }

    public class NftOrderDetailsData
    {
        public int count { get; set; }
        public List<NftOrderDetails> nfts { get; set; }
    }

    public class NftOrderDetailsQuery
    {
        public int page { get; set; }
        public int size { get; set; }
        public string order_id { get; set; }
    }

    public class TransferNft
    {
        public string opteration_type { get; set; }
        public string network_name { get; set; }
        public string collection_address { get; set; }
        public string collection_name { get; set; }
        public string quantity { get; set; }
        public string recipient_address { get; set; }
        public string nft_address { get; set; }
    }

    public class SOLTransaction 
    {
        public List<Instruction> instructions { get; set; }  
        public List<string> signers { get; set; }
    }

    public class Instruction
    {
        public List<AccountMeta> keys { get; set; }
        public string programId { get; set; }
        public List<int> data { get; set; }
    }

    public class AccountMeta
    {
        public string pubkey { get; set; }      
        public bool isSigner { get; set; }     
        public bool isWritable { get; set; }      
    }

    public class EVMTransaction 
    {
        public string from { get; set; }
        public string to { get; set; }
        public string data { get; set; }
        public string value { get; set; }
    }

    public class AptosTransaction
    {
        public List<AptosTransactionItem> transactions { get; set; }

    }

    public class AptosTransactionItem
    {
        public string function { get; set; }
        public string[] typeArguments { get; set; }
        public string[] functionArguments { get; set; }
    }


}

