# CrossCulturalDriving2021

Updated version of CrossCulturalDriving including ECS, new networking stack(MLAPI), Unity.InputSystem, and Unity XR Interaction Toolkit with Oculus Plugin alongside Oculus Integration, works with Logitech G29 steering wheel.

Unity Issues: The Mono Cecil package will not be imported with git, it needs to be manually installed from safe mode.

Oculus Issues: After first time install of the Oculus software, reboot your pc.

Steering Wheel Issues: Logitech GHub may fail to install correctly, one sign of this is that it will seemingly install but then nothing will happen. If this happens close all logitech tasks in task manager, disconnect the steering wheel, disconnect steering wheel from wall power, and reinstall GHub. Also, go to OS:C/ProgramData/LGHUB/depots/96866 and in the logi joy and logi usb folders right click on the .inf files and install them again. When you start the computer, plug in the steering wheel before opening GHub.

Current version is running the inputs for the Logitech G29 with the Logitech SDK input system. To use the other input systems, go to Assets/Scripts/BaseScripts/Player_Drive_System
and comment out the G29 code and uncomment the relevant labeled sections in the OnCreate() and OnUpdate() functions. To use the G29 input systems, you will need to install and run
Logitech Gaming Software and Logitech G Hub and also CLOSE them both before entering play mode(otherwise it will not run and will crash on exiting play mode). Make sure the wheel shows up in Logitech G Hub before closing G Hub. To use the Oculus Quest 2 headset, open Oculus Link through the desktop and also open it on the Quest 2 and leave both open.

## Notes
These notes concern the simulator in development and are subject to change. The standard operating procedure is documented on overleaf. Ask @DavidGoedicke for access.
### Setup
Git: 
Once you pulled the repository you need to initialize the submodule ReRun:
`git submodule update --init --recursive`

Unity:
import the necessary third party packages:
[Runtime File Browser](https://assetstore.unity.com/packages/tools/gui/runtime-file-browser-113006)

### What to keep in mind while running a study
#### Keycodes

| Key combination | Effect                            |
|-----------------|-----------------------------------|
|⇧ Shift +  D | Switch into driving mode |
|⇧ Shift +  W  | Switch back into the waiting room |
|⇧ Shift +  Q | Display SA Questionnaire |
|⇧ Shift +  A/B | Toggle the participants steerring wheel button remotely (for testing questionnaire) |
|0,1,2,3,4 | Switch between the views in the server |


#### Calibration
The participant has to look straight ahead when the calibration button on the server is pressed.
During the calibration, which lasts a few seconds, both hands should be placed on the top of the steering wheel. 

#### Participant controls during driving
The blinkers are located behind the steering wheel left and right. 
The questionnaire can be clicked with the round steering wheel button and head pose. 

#### IP 
The servers local IP is set in the participants VR-UI. Ensure that no firewall is blocking local network access. 

### What to keep in mind while developing a scenario
The individual scenarios need to contain the ScenarioManager prefab. SpawnPoints denote the starting positions of the participants. Trigger Boxes are the end zones of the scenario and start of the questionnaire. 
RoadNetworks can be built with theEasyRoads3D assets. The networked traffic light prefab will change the status for all participants at once. 
Scenes need to be included in the build and included in the Connection and Spawning script (inside Network Manager on the GameManagment Scene)

### Analysis
Rerun .replay files are automatically stored on the server path: `C:\Users\<USERNAME>\AppData\LocalLow\<USERNAME>\XCDriving\test`
