using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class RosCompressedImageToRawImage : MonoBehaviour
{
    [Header("ROS")]
    public string topicName = "/image_raw/compressed";

    [Header("UI")]
    public RawImage targetRawImage;

    Texture2D _tex;

    void Start()
    {
        if (targetRawImage == null)
        {
            Debug.LogError("Asigna el RawImage en el Inspector.");
            return;
        }

        // Se suscribe al topic (ROS-TCP-Connector)
        ROSConnection.GetOrCreateInstance().Subscribe<CompressedImageMsg>(topicName, OnImage);
    }

    void OnImage(CompressedImageMsg msg)
    {
        // msg.data es JPEG/PNG comprimido
        byte[] data = msg.data;

        if (_tex == null)
            _tex = new Texture2D(2, 2, TextureFormat.RGB24, false);

        // LoadImage decodifica JPEG/PNG
        if (_tex.LoadImage(data))
        {
            targetRawImage.texture = _tex;
        }
        else
        {
            Debug.LogWarning("No se pudo decodificar la imagen comprimida.");
        }
    }
}
