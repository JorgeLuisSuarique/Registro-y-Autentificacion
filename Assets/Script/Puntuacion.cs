using System.IO;
using TMPro;
using UnityEngine;

public class Puntuacion : MonoBehaviour
{
    [System.Serializable]
    public class DatosUsuario
    {
        public string _id;
        public string username;
        public bool estado;
        public DatosScore data;
    }

    [System.Serializable]
    public class DatosScore
    {
        public int score;
    }

    private const string RutaUsuarios = "/usuarios";
    private const string NombreUsuarioInvitado = "usuario_invitado";

    private DatosUsuario usuarioActual;
    private string archivoUsuario;
    private bool cambiosPendientes;

    [SerializeField] private TextMeshProUGUI textoCanvas;

    private void Start()
    {
        AsegurarDirectorioUsuarios();

        Debug.Log("Puntuacion lista. Esperando sincronizacion desde AppManager.");

        archivoUsuario = ObtenerRutaUsuario(NombreUsuarioInvitado);

        if (File.Exists(archivoUsuario))
        {
            CargarUsuario();
        }
        else
        {
            CrearNuevoUsuarioInvitado();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            GuardarCambiosSiExisten();
        }
    }

    private void OnApplicationQuit()
    {
        GuardarCambiosSiExisten();
    }

    private void OnDisable()
    {
        GuardarCambiosSiExisten();
    }

    private void AsegurarDirectorioUsuarios()
    {
        string directorioUsuarios = Application.persistentDataPath + RutaUsuarios;

        if (!Directory.Exists(directorioUsuarios))
        {
            Directory.CreateDirectory(directorioUsuarios);
        }
    }

    private void CrearNuevoUsuarioInvitado()
    {
        usuarioActual = CrearUsuarioBase("usuario_" + System.DateTime.Now.Ticks);
        cambiosPendientes = true;
        GuardarCambiosSiExisten();
        MostrarDatosEnCanvas();
        Debug.Log("Nuevo usuario creado: " + usuarioActual.username);
    }

    private DatosUsuario CrearUsuarioBase(string username)
    {
        return new DatosUsuario
        {
            _id = System.Guid.NewGuid().ToString().Substring(0, 16),
            username = username,
            estado = true,
            data = new DatosScore { score = 0 }
        };
    }

    private void CargarUsuario()
    {
        try
        {
            string jsonContent = File.ReadAllText(archivoUsuario);
            usuarioActual = JsonUtility.FromJson<DatosUsuario>(jsonContent);
            NormalizarUsuarioActual();
            cambiosPendientes = false;
            MostrarDatosEnCanvas();
            Debug.Log("Usuario cargado: " + usuarioActual.username + " | Score: " + usuarioActual.data.score);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al cargar usuario: " + e.Message);
            usuarioActual = CrearUsuarioBase("usuario_" + System.DateTime.Now.Ticks);
            cambiosPendientes = true;
            GuardarCambiosSiExisten();
            MostrarDatosEnCanvas();
        }
    }

    private void NormalizarUsuarioActual()
    {
        if (usuarioActual == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(usuarioActual._id))
        {
            usuarioActual._id = System.Guid.NewGuid().ToString().Substring(0, 16);
        }

        if (string.IsNullOrWhiteSpace(usuarioActual.username))
        {
            usuarioActual.username = NombreUsuarioInvitado;
        }

        if (usuarioActual.data == null)
        {
            usuarioActual.data = new DatosScore { score = 0 };
        }
    }

    private void MostrarDatosEnCanvas()
    {
        if (textoCanvas != null && usuarioActual != null)
        {
            textoCanvas.text = "Usuario: " + usuarioActual.username + " Puntuacion: " + usuarioActual.data.score;
        }
    }

    private void GuardarCambiosSiExisten()
    {
        if (!cambiosPendientes || usuarioActual == null || string.IsNullOrWhiteSpace(archivoUsuario))
        {
            return;
        }

        GuardarUsuario();
        cambiosPendientes = false;
    }

    private void GuardarUsuario()
    {
        try
        {
            string jsonContent = JsonUtility.ToJson(usuarioActual, true);
            File.WriteAllText(archivoUsuario, jsonContent);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al guardar usuario: " + e.Message);
        }
    }

    public void ActualizarPuntuacion(int nuevaPuntuacion)
    {
        if (usuarioActual == null)
        {
            Debug.LogWarning("No hay un usuario cargado para actualizar la puntuacion.");
            return;
        }

        int puntuacionNormalizada = Mathf.Max(0, nuevaPuntuacion);

        if (usuarioActual.data.score == puntuacionNormalizada)
        {
            return;
        }

        usuarioActual.data.score = puntuacionNormalizada;
        cambiosPendientes = true;
        GuardarCambiosSiExisten();
        MostrarDatosEnCanvas();
        Debug.Log("Puntuacion actualizada a: " + puntuacionNormalizada);
    }

    public void SumarPuntuacion(int puntos)
    {
        if (puntos <= 0)
        {
            Debug.LogWarning("La cantidad a sumar debe ser mayor que 0.");
            return;
        }

        ActualizarPuntuacion(ObtenerScoreActual() + puntos);
    }

    public void RestarPuntuacion(int puntos)
    {
        if (puntos <= 0)
        {
            Debug.LogWarning("La cantidad a restar debe ser mayor que 0.");
            return;
        }

        ActualizarPuntuacion(ObtenerScoreActual() - puntos);
    }

    public int ObtenerScoreActual()
    {
        if (usuarioActual == null || usuarioActual.data == null)
        {
            return 0;
        }

        return usuarioActual.data.score;
    }

    public DatosUsuario ObtenerUsuario()
    {
        return usuarioActual;
    }

    public void InicializarConUsuario(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("Se intento inicializar Puntuacion con un username vacio.");
            return;
        }

        AsegurarDirectorioUsuarios();
        GuardarCambiosSiExisten();

        archivoUsuario = ObtenerRutaUsuario(username);

        if (File.Exists(archivoUsuario))
        {
            CargarUsuario();
            return;
        }

        usuarioActual = CrearUsuarioBase(username);
        cambiosPendientes = true;
        GuardarCambiosSiExisten();
        MostrarDatosEnCanvas();
        Debug.Log("Nuevo usuario creado para: " + username);
    }

    private string ObtenerRutaUsuario(string username)
    {
        return Application.persistentDataPath + RutaUsuarios + "/" + username + ".json";
    }
}
