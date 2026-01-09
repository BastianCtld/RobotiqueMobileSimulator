# RobotiqueMobileSimulator
I created this simulator for a class project. We had lidar-equipped rovers at our disposition and were instructed to create a LABVIEW program to control it in the classroom.

There were only two rovers shared for the class, but everyone of us constantly needed them to test and troubleshoot our control program.

To solve this issue, I decided to create a digital twin of the rover in Unity, which connects to the LABVIEW control program and communicate with it in the same way the real rover does (sending LiDAR data and receiving movement instructions over TCP).

This simulator was cobbled together in two days, but it allowed us to work on the logic of our control programs without needing access to the real rover.

# Le programme Python
A Python script is available to test the simulator. It is the "main.py" file available here.
The script is a very basic implementation of a SLAM algorithm. Add waypoints on the map that opens by simply left-clicking. Once enough (3 by default) points are placed, the rover will start following the path they form.
