# CrossCulturalDriving

The version of the simulator used for the article: [Strangers in a Strange Land: New Experimental System for Understanding Driving Culture Using VR](https://ieeexplore.ieee.org/document/9720119) can be found under the [`main-OculusCV1`](https://github.com/FAR-Lab/CrossCulturalDriving/tree/main-OculusCV1) branch.

Updated version of CrossCulturalDriving ~~including ECS~~, new networking stack(MLAPI), ~~Unity.InputSystem~~, and ~~Unity XR Interaction Toolkit~~ with Oculus Plugin alongside Oculus Integration, works with Logitech G29 steering wheel. The visual scene is rendered on the standalone headsets. They connect wirelessly to the server which manages the scenario progression, questionnaire and data logging. 

## Notes
These notes concern the simulator in development and are subject to change. The standard operating procedure is documented on Google Docs/Overleaf. Ask Hauke Sandhaus or David Goeddicke for access. 

## Setup for development
Git: 
Once you pulled the repository you need to initialize the submodule ReRun:
`git submodule update --init --recursive`

Unity:
import the necessary third party packages:
[Ultimate Replay](https://assetstore.unity.com/packages/tools/camera/ultimate-replay-2-0-178602)
[Runtime File Browser](https://assetstore.unity.com/packages/tools/gui/runtime-file-browser-113006)
[Oculus Integration](https://developer.oculus.com/downloads/package/unity-integration)

### What to keep in mind while developing a scenario
The individual scenarios need to contain the ScenarioManager prefab. SpawnPoints denote the starting positions of the participants. Trigger Boxes are the end zones of the scenario and start of the questionnaire. 
RoadNetworks can be built with theEasyRoads3D assets. The networked traffic light prefab will change the status for all participants at once. 
Scenes need to be included in the build and included in the Connection and Spawning script (inside Network Manager on the GameManagment Scene)

## Using executable versions
Releases can be found in https://github.com/FAR-Lab/CrossCulturalDriving/releases. 
Install these to the oculus headset through the oculus developer hub. Run the server from the executable file in the zipped folder. 

#### IP 
The servers local IP is set in the participants VR-UI. Ensure that no firewall is blocking local network access. 

### What to keep in mind while running a study
#### Keycodes

| Key combination | Effect                            |
|-----------------|-----------------------------------|
|⇧ Shift +  D | Switch into driving mode |
|⇧ Shift +  W  | Switch back into the waiting room |
|⇧ Shift +  Q | Display SA Questionnaire |
|⇧ Shift +  T | Resets the Timer |
|⇧ Shift +  A/B | Toggle the participants steerring wheel button remotely (for testing questionnaire) |
|0,1,2,3,4 | Switch between the views in the server |
|^ Ctrl + ⎵ Space + A/B|Resets the participant to the starting position|


#### Calibration
The participant has to look straight ahead when the calibration button on the server is pressed.
During the calibration, which lasts a few seconds, both hands should be placed on the top of the steering wheel. 

#### Participant controls during driving
The blinkers are located behind the steering wheel left and right. 
The questionnaire can be clicked with the round steering wheel button and head pose. 
The traffic lights are controlled through the interface on the server. 


### Analysis
Rerun .replay files are automatically stored on the server path: `C:\Users\<USERNAME>\AppData\LocalLow\<USERNAME>\XCDriving\test`


# License
Currently the project is not licensed for use. The scenes use assets from the Unity Asset Store under the [standard license](https://unity.com/legal/as-terms). 
