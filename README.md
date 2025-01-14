Download Unity: Make sure you have Unity installed with a minimum version of 2022.3.11f1. You can download the Unity Hub from the Unity website and then install the appropriate version from within Unity Hub.
Clone the repository.
In Unity Hub, go to the Projects tab.
Select Open or Add (depending on your Unity Hub version).
Browse to the project folder you downloaded or cloned.
Select the project folder (it should contain folders like Assets, Packages, and ProjectSettings).

Required Dependencies
Add the following to your Packages/manifest.json file if required:
"dependencies": {
    "com.unity.nuget.newtonsoft-json": "3.0.2"

}

Run Unity > Assets > Refresh to install dependencies.

Configure SDK Credentials
Obtain your API keys and credentials from https://dashboard.okto.tech/.
Set up google cloud console
1. Google Cloud Console
2. https://sdk-docs.okto.tech/guide/google-authentication-setup
Create a “Resources” folder in Assets
Go to Unity>SDK>Credentials Manager
Press on “Create New Credentials” and input the API key from the Okto Dashboard, Client Id and Client secret key from the Google Cloud Console.

