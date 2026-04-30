using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

[RequireComponent(typeof(Slider))]
public class JointStateToSlider : MonoBehaviour
{
    [Header("ROS")]
    public string topicName = "/joint_states";
    public string jointName = "";     // si está vacío, usa el primer joint que llegue

    [Header("Mapping")]
    public float jointMin = -1.57f;   // rad, ajusta a tu robot
    public float jointMax =  1.57f;   // rad, ajusta a tu robot

    [Header("Smoothing")]
    [Range(0f, 1f)]
    public float lerp = 0.25f;        // 0 = instantáneo, 1 = muy suave

    Slider _slider;
    float _target01 = 0f;
    int _idx = -1;                    // índice del joint en el mensaje

    void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    void Start()
    {
        ROSConnection.GetOrCreateInstance()
            .Subscribe<JointStateMsg>(topicName, OnMsg);
    }

    void Update()
    {
        // suavizado visual
        _slider.value = Mathf.Lerp(_slider.value, _target01, 1f - Mathf.Pow(1f - lerp, Time.deltaTime * 60f));
    }

    void OnMsg(JointStateMsg msg)
    {
        if (msg == null || msg.name == null || msg.position == null) return;
        if (msg.name.Length == 0 || msg.position.Length == 0) return;

        // resolver índice una vez (o cuando no exista)
        if (_idx < 0)
        {
            if (string.IsNullOrEmpty(jointName))
            {
                _idx = 0;
                jointName = msg.name[0];
            }
            else
            {
                for (int i = 0; i < msg.name.Length; i++)
                {
                    if (msg.name[i] == jointName) { _idx = i; break; }
                }
            }
        }

        if (_idx < 0 || _idx >= msg.position.Length) return;

        float pos = (float)msg.position[_idx];

        // normalizar a 0..1
        float t = Mathf.InverseLerp(jointMin, jointMax, pos);
        _target01 = Mathf.Clamp01(t);
    }
}