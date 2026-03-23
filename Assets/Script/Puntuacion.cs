using UnityEngine;
using System.IO;
using TMPro;

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

    private DatosUsuario usuarioActual;
    private string rutaUsuarios = "/usuarios";
    private string archivoUsuario;
    
    [SerializeField] private TextMeshProUGUI textoCanvas;

    void Start()
    {
        // Crear la ruta si no existe
        if (!Directory.Exists(Application.persistentDataPath + rutaUsuarios))
        {
            Directory.CreateDirectory(Application.persistentDataPath + rutaUsuarios);
        }

        // Ahora esperamos que AppManager llame a InicializarConUsuario()
        // Si no se llama, inicializamos vacío
        Debug.Log("Puntuacion lista. Esperando sincronización desde AppManager.");

        archivoUsuario = Application.persistentDataPath + rutaUsuarios + "/usuario.json";

        // Verificar si el archivo existe
        if (File.Exists(archivoUsuario))
        {
            // Si existe, cargar el JSON
            CargarUsuario();
        }
        else
        {
            // Si está vacío, crear uno nuevo
            CrearNuevoUsuario();
        }
    }

    void Update()
    {
        // Actualizar y guardar el estado del usuario cada frame (opcional: hacerlo con timeout)
        if (usuarioActual != null)
        {
            ActualizarYGuardarEstado();
            MostrarDatosEnCanvas();
        }
    }

    private void CrearNuevoUsuario()
    {
        // Crear un nuevo usuario con datos por defecto
        usuarioActual = new DatosUsuario
        {
            _id = System.Guid.NewGuid().ToString().Substring(0, 16), // ID único
            username = "usuario_" + System.DateTime.Now.Ticks,
            estado = true,
            data = new DatosScore { score = 0 }
        };

        // Guardar el nuevo usuario
        GuardarUsuario();
        Debug.Log("Nuevo usuario creado: " + usuarioActual.username);
    }

    private void CargarUsuario()
    {
        try
        {
            string jsonContent = File.ReadAllText(archivoUsuario);
            usuarioActual = JsonUtility.FromJson<DatosUsuario>(jsonContent);
            Debug.Log("Usuario cargado: " + usuarioActual.username + " | Score: " + usuarioActual.data.score);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al cargar usuario: " + e.Message);
            CrearNuevoUsuario();
        }
    }

    private void ActualizarYGuardarEstado()
    {
        // Aquí puedes agregar lógica para actualizar el score u otros datos
        // Por ahora, simplemente guardamos el estado actual
        GuardarUsuario();
    }

    private void MostrarDatosEnCanvas()
    {
        if (textoCanvas != null && usuarioActual != null)
        {
            textoCanvas.text = "Usuario: " + usuarioActual.username + " Puntuación: " + usuarioActual.data.score;
        }
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

    // Método público para actualizar la puntuación
    public void ActualizarPuntuacion(int nuevaPuntuacion)
    {
        if (usuarioActual != null)
        {
            usuarioActual.data.score = nuevaPuntuacion;
            GuardarUsuario();
            Debug.Log("Puntuación actualizada a: " + nuevaPuntuacion);
        }
    }

    // Método público para obtener el usuario actual
    public DatosUsuario ObtenerUsuario()
    {
        return usuarioActual;
    }

        // Método para inicializar con un usuario específico (llamado desde AppManager)
    public void InicializarConUsuario(string username)
    {
        // Actualizar la ruta del archivo con el username específico
        archivoUsuario = Application.persistentDataPath + rutaUsuarios + "/" + username + ".json";

        // Verificar si el archivo de este usuario existe
        if (File.Exists(archivoUsuario))
        {
            // Cargar el usuario existente
            CargarUsuario();
        }
        else
        {
            // Crear un nuevo usuario con los datos del username
            usuarioActual = new DatosUsuario
            {
                _id = System.Guid.NewGuid().ToString().Substring(0, 16),
                username = username,
                estado = true,
                data = new DatosScore { score = 0 }
            };
            GuardarUsuario();
            Debug.Log("Nuevo usuario creado para: " + username);
        }
    }
}
