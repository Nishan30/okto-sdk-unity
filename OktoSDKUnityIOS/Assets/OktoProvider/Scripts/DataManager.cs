using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public string AuthToken { get; set; }
    public string RefreshToken { get; set; }
    public string DeviceToken { get; set; }
    public string apiKey { get; set; }
    public string buildStage { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
        }
        else
        {
            Instance = this; 
            DontDestroyOnLoad(gameObject); 
        }
    }
}
