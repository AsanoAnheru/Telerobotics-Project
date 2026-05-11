// Este programa es el responsable de todo el movimiento del robot
// Tanto el de movimiento como el de ambas herramientas
// Proceso de automatizacion incluido

using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry; 
using RosMessageTypes.Sensor; 
using System.Collections; // Necesario para las Corrutinas

public class ControladorRobot : MonoBehaviour
{
    private ROSConnection ros;
    
    [Header("Configuración ROS")]
    public string topicName = "/cmd_vel"; 
    public string topicHerramienta = "/joint_states";

    [Header("Control Herramienta (2J1)")]
    public Slider sliderHerramienta;
    public string nombreJoint1 = "2J1";
    public float velocidadSlider = 5000f; 
    public float velocidadSubida = 500f; 

    [Header("Control Taladro (2J2)")]
    public string nombreJoint2 = "2J2";
    public float pasoTaladro = 300f; 
    public float limiteMinimo = -10000f;
    public float limiteMaximo = 10000f;
    
    [Tooltip("La velocidad a la que el taladro intentará alcanzar la posición")]
    public float velocidadTaladro = 200f; 
    private float posicionTaladro = 0f; 

    [Header("Automatización Desatornillado")]
    [Tooltip("Velocidad a la que avanza la posición del taladro en modo automático")]
    private bool enProcesoAutomatico = false;
    private bool retrocediendo = false;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TwistMsg>(topicName);
        ros.RegisterPublisher<JointStateMsg>(topicHerramienta);

        if (sliderHerramienta != null)
        {
            sliderHerramienta.minValue = 0f;
            sliderHerramienta.maxValue = 400000f;
        }
    }

    void Update()
    {
        // Lógica 2J1 (Slider)
        float valorSlider = 0f;
        if (sliderHerramienta != null)
        {
            if (Input.GetKey(KeyCode.UpArrow)) sliderHerramienta.value += velocidadSlider * Time.deltaTime;
            if (Input.GetKey(KeyCode.DownArrow)) sliderHerramienta.value -= velocidadSlider * Time.deltaTime;
            valorSlider = sliderHerramienta.value;
        }

        // Lógica 2J2 (Taladro) - Solo funciona si no estamos en medio de la automatización
        if (!enProcesoAutomatico)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                posicionTaladro += pasoTaladro;
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                posicionTaladro -= pasoTaladro;
            }

            // Si la posición toca o supera los límites, vuelve a 0
            if (posicionTaladro >= limiteMaximo || posicionTaladro <= limiteMinimo)
            {
                posicionTaladro = 0f;
            }

            // Publicar Herramienta (2J1)
            JointStateMsg mensajeHerramienta = new JointStateMsg();
            mensajeHerramienta.name = new string[] { nombreJoint1 };
            mensajeHerramienta.position = new double[] { (double)valorSlider };
            mensajeHerramienta.velocity = new double[] { (double)velocidadSubida };
            mensajeHerramienta.effort = new double[] { 0.0 }; 
            
            ros.Publish(topicHerramienta, mensajeHerramienta);

            // Publicar Herramienta (2J2) 
            JointStateMsg mensajeTaladro = new JointStateMsg();
            mensajeTaladro.name = new string[] { nombreJoint2 };
            mensajeTaladro.position = new double[] { (double)posicionTaladro };
            mensajeTaladro.velocity = new double[] { (double)velocidadTaladro };
            mensajeTaladro.effort = new double[] { 0.0 }; 
            
            ros.Publish(topicHerramienta, mensajeTaladro);

            
        }
        // Movimiento de la Base
        ActualizarMovimientoBase();
        
    }

    void ActualizarMovimientoBase()
    {
        float x = 0, y = 0, z = 0;

        // Si estamos en la fase de retroceso automático, forzamos el movimiento hacia atrás
        if (retrocediendo)
        {
            x = -0.1f; // Equivale a pulsar la tecla 'S'
        }
        else // Control manual habitual
        {
            if (Input.GetKey(KeyCode.W)) x = 0.1f;
            if (Input.GetKey(KeyCode.S)) x = -0.1f;
            if (Input.GetKey(KeyCode.A)) y = 0.1f;
            if (Input.GetKey(KeyCode.D)) y = -0.1f;
            if (Input.GetKey(KeyCode.LeftArrow)) z = 0.3f;
            if (Input.GetKey(KeyCode.RightArrow)) z = -0.3f;
        }

        TwistMsg cmd = new TwistMsg();
        cmd.linear.x = x; cmd.linear.y = y; cmd.angular.z = z;
        ros.Publish(topicName, cmd);
    }


    // LÓGICA DE AUTOMATIZACIÓN

    public void IniciarDesatornillado()
    {
        // Evita que se inicie la rutina si ya está en proceso
        if (!enProcesoAutomatico)
        {
            StartCoroutine(SecuenciaDesatornillar());
        }
    }

    private IEnumerator SecuenciaDesatornillar()
    {
        enProcesoAutomatico = true;

        // Posición máxima al taladro
        posicionTaladro = -9999f;

        // Publicar Herramienta (2J2) 
        JointStateMsg mensajeTaladro = new JointStateMsg();
        mensajeTaladro.name = new string[] { nombreJoint2 };
        mensajeTaladro.position = new double[] { (double)posicionTaladro };
        mensajeTaladro.velocity = new double[] { (double)velocidadTaladro };
        mensajeTaladro.effort = new double[] { 0.0 }; 

        ros.Publish(topicHerramienta, mensajeTaladro);

        // Esperar 60 segundos (1 minuto) a que termine físicamente
        yield return new WaitForSeconds(60f);

        // Mover el robot hacia atrás durante 1 segundo
        retrocediendo = true;
        yield return new WaitForSeconds(1f); 

        // Detener movimiento hacia atrás y reiniciar el taladro a 0
        retrocediendo = false;
        posicionTaladro = 0f;
                
        // Publicar Herramienta (2J2) 
        mensajeTaladro.name = new string[] { nombreJoint2 };
        mensajeTaladro.position = new double[] { (double)posicionTaladro };
        mensajeTaladro.velocity = new double[] { (double)velocidadTaladro };
        mensajeTaladro.effort = new double[] { 0.0 }; 

        ros.Publish(topicHerramienta, mensajeTaladro);
        
        enProcesoAutomatico = false;
    }
}
