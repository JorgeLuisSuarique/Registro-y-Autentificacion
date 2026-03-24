using UnityEngine;
using System.IO;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

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
        public int record; // Añadido: mejor puntuación histórica
        public int partidasJugadas; // Añadido: contador de partidas
    }

    private DatosUsuario usuarioActual;
    private string rutaUsuarios = "/usuarios";
    private string archivoUsuario;
    private bool sincronizando = false; // Evita múltiples sincronizaciones simultáneas
    
    [SerializeField] private TextMeshProUGUI textoCanvas;
    [SerializeField] private bool sincronizarAutomaticamente = true; // Opción para controlar sincronización

    void Start()
    {
        if (!Directory.Exists(Application.persistentDataPath + rutaUsuarios))
        {
            Directory.CreateDirectory(Application.persistentDataPath + rutaUsuarios);
        }

        Debug.Log("Puntuacion lista. Esperando sincronización desde AppManager.");
        archivoUsuario = Application.persistentDataPath + rutaUsuarios + "/usuario.json";

        if (File.Exists(archivoUsuario))
        {
            CargarUsuario();
        }
        else
        {
            CrearNuevoUsuario();
        }
    }

    void Update()
    {
        if (usuarioActual != null)
        {
            MostrarDatosEnCanvas();
        }
    }

    private void CrearNuevoUsuario()
    {
        usuarioActual = new DatosUsuario
        {
            _id = System.Guid.NewGuid().ToString().Substring(0, 16),
            username = "usuario_" + System.DateTime.Now.Ticks,
            estado = true,
            data = new DatosScore { score = 0, record = 0, partidasJugadas = 0 }
        };

        GuardarUsuario();
        Debug.Log("Nuevo usuario creado: " + usuarioActual.username);
    }

    private void CargarUsuario()
    {
        try
        {
            string jsonContent = File.ReadAllText(archivoUsuario);
            usuarioActual = JsonUtility.FromJson<DatosUsuario>(jsonContent);
            
            // Compatibilidad con versiones anteriores (si no tienen los nuevos campos)
            if (usuarioActual.data.record == 0 && usuarioActual.data.score > 0)
            {
                usuarioActual.data.record = usuarioActual.data.score;
            }
            
            Debug.Log($"Usuario cargado: {usuarioActual.username} | Score: {usuarioActual.data.score} | Record: {usuarioActual.data.record}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al cargar usuario: " + e.Message);
            CrearNuevoUsuario();
        }
    }

    private void MostrarDatosEnCanvas()
    {
        if (textoCanvas != null && usuarioActual != null)
        {
            textoCanvas.text = $"Usuario: {usuarioActual.username}\nPuntuación: {usuarioActual.data.score}\nRécord: {usuarioActual.data.record}";
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

    // ==================== MÉTODOS PÚBLICOS PARA ACTUALIZAR SCORES ====================

    /// <summary>
    /// Método principal para actualizar puntuación (suma puntos al score actual)
    /// </summary>
    public void SumarPuntos(int puntos)
    {
        if (usuarioActual == null) return;
        
        int nuevoScore = usuarioActual.data.score + puntos;
        ActualizarPuntuacionCompleta(nuevoScore);
    }

    /// <summary>
    /// Método para establecer una puntuación específica
    /// </summary>
    public void EstablecerPuntuacion(int nuevaPuntuacion)
    {
        if (usuarioActual == null) return;
        ActualizarPuntuacionCompleta(nuevaPuntuacion);
    }

    /// <summary>
    /// Método para incrementar partidas jugadas
    /// </summary>
    public void IncrementarPartidasJugadas()
    {
        if (usuarioActual == null) return;
        
        usuarioActual.data.partidasJugadas++;
        GuardarUsuario();
        Debug.Log($"Partidas jugadas: {usuarioActual.data.partidasJugadas}");
        
        // Sincronizar si es necesario (opcional)
        if (sincronizarAutomaticamente)
        {
            string token = PlayerPrefs.GetString("Token", "");
            if (!string.IsNullOrEmpty(token))
            {
                StartCoroutine(SincronizarScoreWeb(usuarioActual.username, usuarioActual.data.score, token));
            }
        }
    }

    /// <summary>
    /// Reinicia la puntuación actual (para nueva partida)
    /// </summary>
    public void ReiniciarPuntuacion()
    {
        if (usuarioActual == null) return;
        
        usuarioActual.data.score = 0;
        GuardarUsuario();
        Debug.Log("Puntuación reiniciada a 0");
        
        MostrarDatosEnCanvas();
        
        // Sincronizar reinicio
        if (sincronizarAutomaticamente)
        {
            string token = PlayerPrefs.GetString("Token", "");
            if (!string.IsNullOrEmpty(token))
            {
                StartCoroutine(SincronizarScoreWeb(usuarioActual.username, 0, token));
            }
        }
    }

    /// <summary>
    /// Método interno que maneja la lógica completa de actualización
    /// </summary>
    private void ActualizarPuntuacionCompleta(int nuevoScore)
    {
        if (usuarioActual == null) return;
        
        int scoreAnterior = usuarioActual.data.score;
        usuarioActual.data.score = nuevoScore;
        
        // Actualizar récord si supera el anterior
        bool esNuevoRecord = nuevoScore > usuarioActual.data.record;
        if (esNuevoRecord)
        {
            usuarioActual.data.record = nuevoScore;
            Debug.Log($"🏆 ¡NUEVO RÉCORD! {usuarioActual.data.record} puntos");
        }
        
        GuardarUsuario();
        Debug.Log($"Puntuación actualizada: {scoreAnterior} → {nuevoScore}");
        
        // Mostrar feedback en canvas si hay evento de nuevo récord
        if (esNuevoRecord && textoCanvas != null)
        {
            StartCoroutine(MostrarMensajeTemporal("¡NUEVO RÉCORD! 🏆", 2f));
        }
        
        // Sincronizar con servidor web si hay token
        string token = PlayerPrefs.GetString("Token", "");
        if (!string.IsNullOrEmpty(token) && sincronizarAutomaticamente)
        {
            StartCoroutine(SincronizarScoreWeb(usuarioActual.username, nuevoScore, token));
        }
    }

    /// <summary>
    /// Muestra un mensaje temporal en el canvas
    /// </summary>
    private IEnumerator MostrarMensajeTemporal(string mensaje, float duracion)
    {
        string textoOriginal = textoCanvas.text;
        textoCanvas.text = mensaje + "\n" + textoOriginal;
        yield return new WaitForSeconds(duracion);
        MostrarDatosEnCanvas();
    }

    // ==================== SINCRONIZACIÓN WEB ====================

    private IEnumerator SincronizarScoreWeb(string username, int score, string token)
    {
        if (sincronizando)
        {
            Debug.Log("Ya hay una sincronización en curso, esperando...");
            yield break;
        }
        
        sincronizando = true;
        
        // Formato para la API
        string json = "{\"data\": {\"score\": " + score + "}}";
        
        using (UnityWebRequest www = new UnityWebRequest("https://sid-restapi.onrender.com/api/usuarios/" + username, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("x-token", token);
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✔ Score sincronizado con el servidor: {score} puntos");
            }
            else
            {
                Debug.LogError($"❌ Error de sincronización: {www.responseCode} - {www.downloadHandler.text}");
                // Aquí podrías implementar un sistema de reintentos o cola offline
            }
        }
        
        sincronizando = false;
    }

    // ==================== MÉTODOS DE CONSULTA ====================

    public DatosUsuario ObtenerUsuario()
    {
        return usuarioActual;
    }
    
    public int ObtenerPuntuacionActual()
    {
        return usuarioActual?.data.score ?? 0;
    }
    
    public int ObtenerRecord()
    {
        return usuarioActual?.data.record ?? 0;
    }
    
    public int ObtenerPartidasJugadas()
    {
        return usuarioActual?.data.partidasJugadas ?? 0;
    }
    
    public bool TieneNuevoRecord(int puntuacion)
    {
        return usuarioActual != null && puntuacion > usuarioActual.data.record;
    }

    // ==================== INICIALIZACIÓN DESDE APPMANAGER ====================

    public void InicializarConUsuario(string username)
    {
        archivoUsuario = Application.persistentDataPath + rutaUsuarios + "/" + username + ".json";
        
        if (File.Exists(archivoUsuario))
        {
            CargarUsuario();
            Debug.Log($"Usuario existente cargado: {username}");
            
            // Opcional: Sincronizar score desde la web al cargar
            string token = PlayerPrefs.GetString("Token", "");
            if (!string.IsNullOrEmpty(token))
            {
                StartCoroutine(ObtenerScoreWeb(username, token));
            }
        }
        else
        {
            usuarioActual = new DatosUsuario
            {
                _id = System.Guid.NewGuid().ToString().Substring(0, 16),
                username = username,
                estado = true,
                data = new DatosScore { score = 0, record = 0, partidasJugadas = 0 }
            };
            GuardarUsuario();
            Debug.Log($"Nuevo usuario creado para: {username}");
        }
        
        MostrarDatosEnCanvas();
    }
    
    /// <summary>
    /// Obtiene el score desde el servidor web (para sincronización inicial)
    /// </summary>
    private IEnumerator ObtenerScoreWeb(string username, string token)
    {
        using (UnityWebRequest www = UnityWebRequest.Get("https://sid-restapi.onrender.com/api/usuarios/" + username))
        {
            www.SetRequestHeader("x-token", token);
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                // Intentar parsear el score desde la respuesta
                try
                {
                    // Asumiendo que la respuesta tiene estructura similar a tu usuario
                    var respuesta = JsonUtility.FromJson<DatosUsuario>(www.downloadHandler.text);
                    if (respuesta != null && respuesta.data != null && respuesta.data.score > usuarioActual.data.score)
                    {
                        usuarioActual.data.score = respuesta.data.score;
                        usuarioActual.data.record = Mathf.Max(usuarioActual.data.record, respuesta.data.score);
                        GuardarUsuario();
                        Debug.Log($"Score sincronizado desde web: {usuarioActual.data.score}");
                        MostrarDatosEnCanvas();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"No se pudo obtener score desde web: {e.Message}");
                }
            }
        }
    }
}
