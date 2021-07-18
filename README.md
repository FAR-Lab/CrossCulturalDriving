# CrossCulturalDriving2021

Updated version of CrossCulturalDriving including ECS, new networking stack(MLAPI), Unity.InputSystem, and new VR stack, works with Logitech G29 steering wheel.

Current version is running the inputs for the Logitech G29 with the logitech SDK input system. To use the other input systems, go to Assets/Scripts/BaseScripts/Player_Drive_System
and comment out the G29 code and uncomment the relevant labeled sections in the OnCreate() and OnUpdate() functions. To use the G29 input systems, you will need to install and run
Logitech Gaming Software and Logitech G Hub and also CLOSED them both before entering play mode(otherwise it will not run and will crash on exiting play mode).
