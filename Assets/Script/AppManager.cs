using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class AppManager : MonoBehaviour
{
    private const string ApiUrl = "https://sid-restapi.onrender.com";

    private string Token;
    private string Username;

    [Header("Referencias Externas")]
    [SerializeField] private Puntuacion puntuacionScript;

    [Header("Panel Login")]
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private TMP_Text loginErrorText;

    [Header("Panel Register")]
    [SerializeField] private GameObject panelRegister;
    [SerializeField] private TMP_InputField registerUsernameInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_Text registerErrorText;

    [Header("Panel Main")]
    [SerializeField] private GameObject panelMain;
    [SerializeField] private TMP_Text welcomeLabel;

    private void Start()
    {
        SetError(loginErrorText, "");
        SetError(registerErrorText, "");

        Token = PlayerPrefs.GetString("Token", "");
        Username = PlayerPrefs.GetString("Username", "");

        if (!string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(Username))
        {
            StartCoroutine(VerifyToken());
            return;
        }

        ShowPanel(panelLogin);
    }

    private IEnumerator VerifyToken()
    {
        UnityWebRequest www = UnityWebRequest.Get(ApiUrl + "/api/usuarios/" + Username);
        www.SetRequestHeader("x-token", Token);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Sesion restaurada: " + Username);
            EnterGame();
        }
        else
        {
            Debug.LogWarning("Token invalido, redirigiendo al login.");
            ClearSession();
            ShowPanel(panelLogin);
        }
    }

    public void RegisterButtonHandler()
    {
        SetError(registerErrorText, "");

        string user = registerUsernameInput.text.Trim();
        string pass = registerPasswordInput.text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetError(registerErrorText, "Completa todos los campos.");
            return;
        }

        StartCoroutine(RegisterCoroutine(user, pass));
    }

    private IEnumerator RegisterCoroutine(string username, string password)
    {
        RegisterData data = new RegisterData { username = username, password = password };
        string json = JsonUtility.ToJson(data);

        UnityWebRequest www = UnityWebRequest.Post(ApiUrl + "/api/usuarios", json, "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            loginUsernameInput.text = username;
            loginPasswordInput.text = password;
            SetError(loginErrorText, "Cuenta creada. Inicia sesion.");
            GoToLoginButtonHandler();
        }
        else
        {
            ErrorResponse err = TryParseError(www.downloadHandler.text);
            SetError(registerErrorText, err != null ? err.msg : "Error al registrar. Intenta otro usuario.");
        }
    }

    public void LoginButtonHandler()
    {
        SetError(loginErrorText, "");

        string user = loginUsernameInput.text.Trim();
        string pass = loginPasswordInput.text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetError(loginErrorText, "Ingresa usuario y contrasena.");
            return;
        }

        StartCoroutine(LoginCoroutine(user, pass));
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        AuthData authData = new AuthData { username = username, password = password };
        string json = JsonUtility.ToJson(authData);

        UnityWebRequest www = UnityWebRequest.Post(ApiUrl + "/api/auth/login", json, "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            AuthResponse response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
            Token = response.token;
            Username = response.usuario.username;

            PlayerPrefs.SetString("Token", Token);
            PlayerPrefs.SetString("Username", Username);
            PlayerPrefs.Save();

            EnterGame();
        }
        else
        {
            ErrorResponse err = TryParseError(www.downloadHandler.text);
            SetError(loginErrorText, err != null ? err.msg : "Usuario o contrasena incorrectos.");
        }
    }

    public void LogoutButtonHandler()
    {
        ClearSession();

        if (welcomeLabel != null)
        {
            welcomeLabel.text = "";
        }

        ShowPanel(panelLogin);
    }

    private void ClearSession()
    {
        Token = "";
        Username = "";

        PlayerPrefs.DeleteKey("Token");
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.Save();
    }

    public void GoToRegisterButtonHandler()
    {
        SetError(registerErrorText, "");
        ShowPanel(panelRegister);
    }

    public void GoToLoginButtonHandler()
    {
        SetError(loginErrorText, "");
        ShowPanel(panelLogin);
    }

    private void ShowPanel(GameObject target)
    {
        panelLogin.SetActive(false);
        panelRegister.SetActive(false);
        panelMain.SetActive(false);

        if (target != null)
        {
            target.SetActive(true);
        }
    }

    private void EnterGame()
    {
        if (welcomeLabel != null)
        {
            welcomeLabel.text = "Bienvenido, " + Username;
        }

        // Si el script de puntuación existe, le decimos qué usuario entró
        if (puntuacionScript != null)
        {
            puntuacionScript.InicializarConUsuario(Username);
        }

        ShowPanel(panelMain);
    }

    private void SetError(TMP_Text label, string message)
    {
        if (label != null)
        {
            label.text = message;
        }
    }

    private ErrorResponse TryParseError(string json)
    {
        try
        {
            return JsonUtility.FromJson<ErrorResponse>(json);
        }
        catch
        {
            return null;
        }
    }
    
}

[System.Serializable] public class AuthData { public string username; public string password; }
[System.Serializable] public class RegisterData { public string username; public string password; }
[System.Serializable] public class User { public string _id; public string username; }
[System.Serializable] public class AuthResponse { public User usuario; public string token; }
[System.Serializable] public class ErrorResponse { public string msg; }
