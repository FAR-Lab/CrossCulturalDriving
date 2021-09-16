# CrossCulturalDriving2021

Updated version of CrossCulturalDriving including ECS, new networking stack(MLAPI), Unity.InputSystem, and Unity XR Interaction Toolkit with Oculus Plugin alongside Oculus Integration, works with Logitech G29 steering wheel.

Unity Issues: The Mono Cecil package will not be imported with git, it needs to be manually installed from safe mode.

Oculus Issues: After first time install of the Oculus software, reboot your pc.

Steering Wheel Issues: Logitech GHub may fail to install correctly, one sign of this is that it will seemingly install but then nothing will happen. If this happens close all logitech tasks in task manager, disconnect the steering wheel, disconnect steering wheel from wall power, and reinstall GHub. Also, go to OS:C/ProgramData/LGHUB/depots/96866 and in the logi joy and logi usb folders right click on the .inf files and install them again.

Current version is running the inputs for the Logitech G29 with the Logitech SDK input system. To use the other input systems, go to Assets/Scripts/BaseScripts/Player_Drive_System
and comment out the G29 code and uncomment the relevant labeled sections in the OnCreate() and OnUpdate() functions. To use the G29 input systems, you will need to install and run
Logitech Gaming Software and Logitech G Hub and also CLOSE them both before entering play mode(otherwise it will not run and will crash on exiting play mode). Make sure the wheel shows up in Logitech G Hub before closing G Hub. To use the Oculus Quest 2 headset, open Oculus Link through the desktop and also open it on the Quest 2 and leave both open.
