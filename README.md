## 🎮 Unity Setup Instructions

To use this Unity project with the ROS2 + Gazebo UR5 simulator, only one script needs to be modified:

### **📌 1. Modify the Joint Controller Script**
All adjustments should be made in:


This script handles all input processing, TCP communication, and joint state updates.

---

## 🔧 2. Install Required Unity Robotics Packages

This project requires the official Unity Robotics packages:

- **URDF Importer**  
- **ROS–TCP Connector**  
- **Unity Robotics Demo** (you don't need to install this again, I have added for you)


Please follow the official installation guide:  
https://github.com/Unity-Technologies/Unity-Robotics-Hub/blob/main/tutorials/ros_unity_integration/setup.md#install-unity-robotics-demo

After installing the packages, make sure:

- The **ROSConnection** component exists in the Unity scene  
- The correct ROS IP + Port are configured  
- The `default_server_endpoint` node is running on the ROS2 side  

---

## 🕹️ 3. Unity Keyboard Controls

When the Unity Play Mode is running and ROS2/Gazebo are connected, control the UR5 using:

### **🔼 Translation Control (Cartesion movement, XYZ)**
| Key | Action |
|-----|--------|
| W | Move +X |
| S | Move –X |
| A | Move +Y |
| D | Move –Y |
| Q | Move +Z |
| E | Move –Z |

### **🔄 Rotation Control (Roll/Pitch/Yaw)**
| Key | Action |
|-----|--------|
| I | Rotate +Roll |
| K | Rotate –Roll |
| J | Rotate +Pitch |
| L | Rotate –Pitch |
| U | Rotate +Yaw |
| O | Rotate –Yaw |

These commands are sent through ROS–TCP Connector → ROS2 → Gazebo → UR5.

Unity acts as the **teleoperation front-end**, while ROS2 controls actual robot motion.

---

## 🤖 4. ROS2 + Gazebo Simulation

For ROS2/Gazebo usage and setup, please refer to the companion repository:

👉 **[UR5_ROS2_Gazebo](https://github.com/GUOGUOGUODING/UR5_ROS2_Gazebo)**

This repository includes:

- ROS2 workspace
- Gazebo simulation environment
- ros2_control setup
- UR5 control launch files
- ROS–Unity TCP endpoint instructions

Unity will not operate correctly unless ROS2 is properly launched on the Gazebo side.

## 🧩 5. Unity Scene Requirements

Before entering Play Mode, make sure your Unity scene contains:

- A UR5 robot imported using **URDF Importer**
- A **ROSConnection** object  
- Correct ROS IP + Port configured (default 10000)
- The `Joint_Controller.cs` script attached to the main controller object

Without these elements, Unity cannot communicate with ROS2.

