//Este programa es el responsable de todo el movimiento del robot
//Tanto el de movimiento como el de ambas herramientas

using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry; 
using RosMessageTypes.Sensor; 

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

        // Lógica 2J2 (Taladro)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            posicionTaladro += pasoTaladro;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            posicionTaladro -= pasoTaladro;
        }


        // Si la posición toca o supera los límites, vuelve a 0
        // De esta manera podemos seguir girando la herramienta una vez llegado al limite
        if (posicionTaladro >= limiteMaximo || posicionTaladro <= limiteMinimo)
        {
            posicionTaladro = 0f;
        }

        // Hay que publicar JointStateMsg por separado

        // Herramienta (2J1)
        JointStateMsg mensajeHerramienta = new JointStateMsg();
        mensajeHerramienta.name = new string[] { nombreJoint1 };
        mensajeHerramienta.position = new double[] { (double)valorSlider };
        mensajeHerramienta.velocity = new double[] { (double)velocidadSubida };
        mensajeHerramienta.effort = new double[] { 0.0 }; 
        
        ros.Publish(topicHerramienta, mensajeHerramienta);

        // Herramienta (2J2) 
        JointStateMsg mensajeTaladro = new JointStateMsg();
        mensajeTaladro.name = new string[] { nombreJoint2 };
        mensajeTaladro.position = new double[] { (double)posicionTaladro };
        mensajeTaladro.velocity = new double[] { (double)velocidadTaladro };
        mensajeTaladro.effort = new double[] { 0.0 }; 
        
        ros.Publish(topicHerramienta, mensajeTaladro);

        // Movimiento de la Base (WASD + Flechas)
        ActualizarMovimientoBase();
    }

    void ActualizarMovimientoBase()
    {
        float x = 0, y = 0, z = 0;
        if (Input.GetKey(KeyCode.W)) x = 0.1f;
        if (Input.GetKey(KeyCode.S)) x = -0.1f;
        if (Input.GetKey(KeyCode.A)) y = 0.1f;
        if (Input.GetKey(KeyCode.D)) y = -0.1f;
        if (Input.GetKey(KeyCode.LeftArrow)) z = 0.3f;
        if (Input.GetKey(KeyCode.RightArrow)) z = -0.3f;

        TwistMsg cmd = new TwistMsg();
        cmd.linear.x = x; cmd.linear.y = y; cmd.angular.z = z;
        ros.Publish(topicName, cmd);
    }
}
