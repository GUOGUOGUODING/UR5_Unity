All we need to adjust is just the "Joint_Controller.cs" in Scripts subfolder in Assets.

you need to download some packages in Unity, including urdf importer, ROS TCP connector, please refer to https://github.com/Unity-Technologies/Unity-Robotics-Hub/blob/main/tutorials/ros_unity_integration/setup.md#install-unity-robotics-demo

When connecting with ROS2 and Gazebo, you can use WSADQE to control the translation of ur5, you can use IKJLUO to control the rotation of ur5 in the play mode of Unity.

For how to use ROS2 and Gazebo, please refer to UR5_ROS2_Gazebo repo.

