using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.UnityRoboticsDemo;

public class JointStateSubscriber : MonoBehaviour
{
    public ArticulationBody[] joints;     
    private ROSConnection ros;
    public string topicName = "pos_rot";
    public float publishMessageFrequency = 0.1f;
    private float timeElapsed;

    private float pos_x = 0.001f;
    private float pos_y = 0.191f;
    private float pos_z = 1.001f;
    Quaternion q_init = new Quaternion(-0.707f, 0.001f, 0.001f, 0.707f);
    private float rot_x = 0.000f;
    private float rot_y = 0.000f;
    private float rot_z = 0.000f;
    public float moveSpeed = 0.2f;
    public float rotSpeed = 20f;
    void Start()
    {
        // var drive1 = joints[1].xDrive;
        // var drive2 = joints[2].xDrive;
        // drive1.target = -90f;
        // drive2.target = 90f;
        // joints[1].xDrive = drive1;
        // joints[2].xDrive = drive2;
        Debug.Log("Start() called");

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PosRotMsg>(topicName);
        ros.Subscribe<JointStateMsg>("/joint_states", JointStateCallback);
    }

    // 3. call back function
    void JointStateCallback(JointStateMsg msg)
    {
        int count = Mathf.Min(joints.Length, msg.position.Length);

        for (int i = 0; i < count; i++)
        {
            float jointDeg = (float)msg.position[i] * Mathf.Rad2Deg;

            // drive the motor to target position
            var drive = joints[i].xDrive;
            drive.target = jointDeg;
            joints[i].xDrive = drive;
        }
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;

        if (Input.GetKey(KeyCode.W)) pos_x += moveSpeed * Time.deltaTime; 
        if (Input.GetKey(KeyCode.S)) pos_x -= moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.A)) pos_y += moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) pos_y -= moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.Q)) pos_z += moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) pos_z -= moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.I)) rot_x += rotSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.K)) rot_x -= rotSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.J)) rot_y += rotSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.L)) rot_y -= rotSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.U)) rot_z += rotSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.O)) rot_z -= rotSpeed * Time.deltaTime;



        Quaternion q_delta = Quaternion.Euler(rot_x, rot_y, rot_z);
        Quaternion q_target = q_delta * q_init;
        if (timeElapsed > publishMessageFrequency)
        {
            PosRotMsg RoboPos = new PosRotMsg(
                pos_x,
                pos_y,
                pos_z,
                q_target.x,
                q_target.y,
                q_target.z,
                q_target.w
            );
            Debug.Log($"Published pos_rot: ({pos_x:F3},{pos_y:F3},{pos_z:F3})");

            ros.Publish(topicName, RoboPos);
            timeElapsed = 0;
        }
    }
}
