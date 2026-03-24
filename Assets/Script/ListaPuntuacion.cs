using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;

public class ListaPuntuacion : MonoBehaviour
{
    [System.Serializable]
    public class DatosUsuario
    {
        public string username;
        public DatosScore data;
    }

    [System.Serializable]
    public class DatosScore
    {
        public int score;
    }

    [Header("Textos de la tabla")]
    [SerializeField] private TextMeshProUGUI[] textosPosicion;
    [SerializeField] private TextMeshProUGUI[] textosUsuario;
    [SerializeField] private TextMeshProUGUI[] textosPuntuacion;
    
    [Header("Configuración")]
    [SerializeField] private int maxUsuariosMostrar = 10;
    [SerializeField] private float tiempoActualizacion = 5f;
    
    private string rutaUsuarios;
    private List<DatosUsuario> listaUsuarios = new List<DatosUsuario>();
    private Coroutine actualizacionAutomatica;

    void Start()
    {
        rutaUsuarios = Application.persistentDataPath + "/usuarios";
        
        if (!Directory.Exists(rutaUsuarios))
            Directory.CreateDirectory(rutaUsuarios);
        
        CargarTabla();
        
        if (tiempoActualizacion > 0)
            actualizacionAutomatica = StartCoroutine(ActualizacionAutomatica());
    }
    
    void OnDestroy()
    {
        if (actualizacionAutomatica != null)
            StopCoroutine(actualizacionAutomatica);
    }
    
    private IEnumerator ActualizacionAutomatica()
    {
        while (true)
        {
            yield return new WaitForSeconds(tiempoActualizacion);
            CargarTabla();
        }
    }
    
    public void RecargarTabla()
    {
        CargarTabla();
    }
    
    private void CargarTabla()
    {
        StartCoroutine(CargarUsuarios());
    }
    
    private IEnumerator CargarUsuarios()
    {
        listaUsuarios.Clear();
        
        yield return null;
        
        try
        {
            string[] archivos = Directory.GetFiles(rutaUsuarios, "*.json");
            
            foreach (string archivo in archivos)
            {
                try
                {
                    string jsonContent = File.ReadAllText(archivo);
                    DatosUsuario usuario = JsonUtility.FromJson<DatosUsuario>(jsonContent);
                    
                    if (usuario != null && !string.IsNullOrEmpty(usuario.username))
                    {
                        listaUsuarios.Add(usuario);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error al leer archivo: {e.Message}");
                }
            }
            
            // Ordenar de mayor a menor puntuación
            listaUsuarios = listaUsuarios.OrderByDescending(u => u.data.score).ToList();
            
            // Mostrar en la tabla
            MostrarTabla();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al cargar usuarios: {e.Message}");
        }
    }
    
    private void MostrarTabla()
    {
        int cantidad = Mathf.Min(listaUsuarios.Count, maxUsuariosMostrar);
        
        for (int i = 0; i < cantidad; i++)
        {
            // Actualizar textos si existen
            if (i < textosPosicion.Length && textosPosicion[i] != null)
                textosPosicion[i].text = (i + 1).ToString();
            
            if (i < textosUsuario.Length && textosUsuario[i] != null)
                textosUsuario[i].text = listaUsuarios[i].username;
            
            if (i < textosPuntuacion.Length && textosPuntuacion[i] != null)
                textosPuntuacion[i].text = listaUsuarios[i].data.score.ToString();
        }
        
        // Limpiar las filas que no se usan
        for (int i = cantidad; i < textosPosicion.Length; i++)
        {
            if (textosPosicion[i] != null) textosPosicion[i].text = "";
            if (textosUsuario[i] != null) textosUsuario[i].text = "";
            if (textosPuntuacion[i] != null) textosPuntuacion[i].text = "";
        }
        
        Debug.Log($"✅ Tabla actualizada: {cantidad} usuarios mostrados");
    }
}