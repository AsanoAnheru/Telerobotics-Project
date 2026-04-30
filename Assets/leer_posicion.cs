using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry; 
using RosMessageTypes.Nav; // Necesario para OdometryMsg

public class EstimacionOdometria : MonoBehaviour
{
    private ROSConnection ros;
    
    [Tooltip("Tópico de donde leemos las velocidades")]
    public string cmdVelTopic = "/cmd_vel";

    [Tooltip("Tópico donde publicaremos nuestra posición estimada")]
    public string odomTopic = "/odom";

    // Variables para almacenar la velocidad actual
    private float velLinealX = 0f;
    private float velLinealY = 0f;
    private float velAngularZ = 0f;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        
        // Nos suscribimos para escuchar comandos
        ros.Subscribe<TwistMsg>(cmdVelTopic, ActualizarVelocidad);
        
        // Nos registramos para publicar la odometría
        ros.RegisterPublisher<OdometryMsg>(odomTopic);
    }

    void ActualizarVelocidad(TwistMsg mensaje)
    {
        // Guardamos las velocidades ordenadas desde ROS
        velLinealX = (float)mensaje.linear.x;
        velLinealY = (float)mensaje.linear.y;
        velAngularZ = (float)mensaje.angular.z;
    }

    void Update()
    {
        // --- 1. ACTUALIZAR POSICIÓN EN UNITY ---
        
        float desplazamientoZ = velLinealX * Time.deltaTime;
        float desplazamientoX = -velLinealY * Time.deltaTime;
        
        // Movemos el robot
        transform.Translate(new Vector3(desplazamientoX, 0, desplazamientoZ), Space.Self);

        float gradosGiroY = -velAngularZ * Mathf.Rad2Deg * Time.deltaTime;
        // Rotamos el robot
        transform.Rotate(0, gradosGiroY, 0, Space.Self);

        // --- 2. PUBLICAR ODOMETRÍA EN ROS ---
        PublicarOdometria();
    }

    void PublicarOdometria()
    {
        // Creamos el mensaje vacío
        OdometryMsg mensajeOdom = new OdometryMsg();

        // Configuramos los nombres de los marcos de referencia (Frames)
        mensajeOdom.header.frame_id = "odom";
        mensajeOdom.child_frame_id = "base_link";

        // CONVERSIÓN DE POSICIÓN: Unity -> ROS
        // ROS X (Adelante) = Unity Z
        // ROS Y (Izquierda) = Unity -X
        // ROS Z (Arriba)   = Unity Y
        mensajeOdom.pose.pose.position.x = transform.position.z;
        mensajeOdom.pose.pose.position.y = -transform.position.x;
        mensajeOdom.pose.pose.position.z = transform.position.y;

        // CONVERSIÓN DE ROTACIÓN: Cuaterniones Unity -> ROS
        mensajeOdom.pose.pose.orientation.x = transform.rotation.z;
        mensajeOdom.pose.pose.orientation.y = -transform.rotation.x;
        mensajeOdom.pose.pose.orientation.z = transform.rotation.y;
        mensajeOdom.pose.pose.orientation.w = -transform.rotation.w;

        // Añadimos también las velocidades al mensaje para que sea una odometría completa
        mensajeOdom.twist.twist.linear.x = velLinealX;
        mensajeOdom.twist.twist.linear.y = velLinealY;
        mensajeOdom.twist.twist.angular.z = velAngularZ;

        // Enviamos el mensaje a la red de ROS
        ros.Publish(odomTopic, mensajeOdom);
    }
}
