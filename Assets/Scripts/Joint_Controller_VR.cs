using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
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

    // Position & Roataion
    private float pos_x = 0.001f;
    private float pos_y = 0.191f;
    private float pos_z = 1.001f;
    Quaternion q_init = new Quaternion(-0.707f, 0.001f, 0.001f, 0.707f);
    private float rot_x = 0.000f;
    private float rot_y = 0.000f;
    private float rot_z = 0.000f;
    public float moveSpeed = 0.2f;
    public float rotSpeed = 20f;

    // XR Controllers
    private List<InputDevice> leftHandDevices = new List<InputDevice>();
    private List<InputDevice> rightHandDevices = new List<InputDevice>();
    private InputDevice leftController;
    private InputDevice rightController;

    void Start()
    {
        Debug.Log("Start() called");

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PosRotMsg>(topicName);
        ros.Subscribe<JointStateMsg>("/joint_states", JointStateCallback);

        InitControllers();
    }

    void InitControllers()
    {
        leftHandDevices.Clear();
        rightHandDevices.Clear();

        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

        if (leftHandDevices.Count > 0)
        {
            leftController = leftHandDevices[0];
            Debug.Log($"Left controller found: {leftController.name}");
        }
        else
        {
            Debug.LogWarning("No left-hand controller found.");
        }

        if (rightHandDevices.Count > 0)
        {
            rightController = rightHandDevices[0];
            Debug.Log($"Right controller found: {rightController.name}");
        }
        else
        {
            Debug.LogWarning("No right-hand controller found.");
        }
    }

    // ROS /joint_states
    void JointStateCallback(JointStateMsg msg)
    {
        int count = Mathf.Min(joints.Length, msg.position.Length);

        for (int i = 0; i < count; i++)
        {
            float jointDeg = (float)msg.position[i] * Mathf.Rad2Deg;

            var drive = joints[i].xDrive;
            drive.target = jointDeg;
            joints[i].xDrive = drive;
        }
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;

        if (!leftController.isValid)
        {
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
            if (leftHandDevices.Count > 0)
                leftController = leftHandDevices[0];
        }

        if (!rightController.isValid)
        {
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
            if (rightHandDevices.Count > 0)
                rightController = rightHandDevices[0];
        }

        // ---------- Keyboard control ----------
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

        // ---------- Left controller: position control ----------
        if (leftController.isValid)
        {
            // Left joystick → WSAD
            if (leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftAxis))
            {
                // Up/Down: X direction
                pos_x += leftAxis.y * moveSpeed * Time.deltaTime;
                // Left/Right: Y direction
                pos_y += leftAxis.x * moveSpeed * Time.deltaTime;
            }

            // Y button → Q (pos_z++)
            if (leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool yButton) && yButton)
            {
                pos_z += moveSpeed * Time.deltaTime;
            }

            // X button → E (pos_z--)
            if (leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool xButton) && xButton)
            {
                pos_z -= moveSpeed * Time.deltaTime;
            }
        }

        // ---------- Right controller: rotation control ----------
        if (rightController.isValid)
        {
            // Right joystick → IKJL
            if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightAxis))
            {
                // Up/Down: rotate around X axis
                rot_x += rightAxis.y * rotSpeed * Time.deltaTime;
                // Left/Right: rotate around Y axis
                rot_y += rightAxis.x * rotSpeed * Time.deltaTime;
            }

            // B button → U (rot_z++)
            if (rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bButton) && bButton)
            {
                rot_z += rotSpeed * Time.deltaTime;
            }

            // A button → O (rot_z--)
            if (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aButton) && aButton)
            {
                rot_z -= rotSpeed * Time.deltaTime;
            }
        }
        // ---------- Compute quaternion & publish ROS ----------
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

            Debug.Log($"Published pos_rot: ({pos_x:F3},{pos_y:F3},{pos_z:F3}) "
                    + $"rot({rot_x:F1},{rot_y:F1},{rot_z:F1})");

            ros.Publish(topicName, RoboPos);
            timeElapsed = 0;
        }
    }
}
