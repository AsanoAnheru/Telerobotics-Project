using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std; 

public class MapaDeLuz : MonoBehaviour
{
    private ROSConnection ros;
    public string topicName = "/light_sensor";
    
    [Tooltip("El prefab de la esfera que actuará como marcador de luz")]
    public GameObject marcadorPrefab; 

    [Tooltip("Tiempo en segundos que debe pasar entre cada marcador")]
    public float tiempoEntreLecturas = 1.0f; 
    
    [Tooltip("Valor máximo del sensor de luz")]
    public float valorMaximoLuz = 1500; 
    
    private float tiempoUltimaLectura = 0f;

    void Start()
    {
        // 1. Obtener la instancia de conexión con ROS y suscribirse al tópico
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<Float32Msg>(topicName, ProcesarLuz);
    }

    void ProcesarLuz(Float32Msg mensaje)
    {
        // 2. Comprobamos el delay de tiempo
        if (Time.time - tiempoUltimaLectura >= tiempoEntreLecturas)
        {
            tiempoUltimaLectura = Time.time;

            // 3. Extraer el valor del sensor
            float intensidadLuz = mensaje.data;
            
            // 4. Instanciar el marcador en la posición actual
            GameObject nuevoMarcador = Instantiate(marcadorPrefab, transform.position, Quaternion.identity);
            
            // 5. Normalizar el color y aplicarlo
            Renderer rend = nuevoMarcador.GetComponent<Renderer>();
            if (rend != null)
            {
                // Dividimos entre el máximo para convertir el rango
                float intensidadNormalizada = intensidadLuz / valorMaximoLuz;
                
                // Aseguramos que el valor no se pase por debajo de 0 ni por encima de 1
                intensidadNormalizada = Mathf.Clamp01(intensidadNormalizada);

                // Aplicamos la mezcla de color (0 = Azul, 1 = Rojo)
                rend.material.color = Color.Lerp(Color.blue, Color.red, intensidadNormalizada);
            }
        }
    }
}