# CrossCulturalDriving

This is the Fall 2023 working version of the VR Driving Simulator for development by Georgia Tech and Cornell Tech.

## Notes
These notes concern the simulator in development and are subject to change. Please reach out to David Geodicke for the most updated version of setup notes. 

Below are the main collaborators on this semester’s iteration of the VR Driving Simulator

David Goedicke - Main point of contact from Cornell Tech. Created the Cornell Tech Driving Simulator. 
Email: dg536@cornell.edu
Discord: @formerlyserializedas_david_

Wendy Ju - Main professor from Cornell Tech
Email: wendyju@cornell.edu
Discord: @wendyju_31298

Bruce Walker - Main Professor from Georgia Tech
Email: bruce.walker@psych.gatech.edu
Discord: @brucenwalker

Kevin Sadi - Master’s Student from Georgia Tech
Email: ksadi3@gatech.edu
Discord: @kekkekin

Radium Zhang - Master’s Student from Georgia Tech
Email: lzhang793@gatech.edu
Discord: @Radium#7542

Saarang Prabhuram - Lab Assistant from Georgia Tech
Email: bruce.walker@psych.gatech.edu
Discord: @saronp23

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

| Key combination        | Effect                                                                              |
| ---------------------- | ----------------------------------------------------------------------------------- |
| ⇧ Shift +  D           | Switch into driving mode                                                            |
| ⇧ Shift +  W           | Switch back into the waiting room                                                   |
| ⇧ Shift +  Q           | Display SA Questionnaire                                                            |
| ⇧ Shift +  T           | Resets the Timer                                                                    |
| ⇧ Shift +  A/B         | Toggle the participants steerring wheel button remotely (for testing questionnaire) |
| 0,1,2,3,4              | Switch between the views in the server                                              |
| ^ Ctrl + ⎵ Space + A/B | Resets the participant to the starting position                                     |


#### Calibration
The participant has to look straight ahead when the calibration button on the server is pressed.
During the calibration, which lasts a few seconds, both hands should be placed on the top of the steering wheel. 

#### Participant controls during driving
The blinkers are located behind the steering wheel left and right. 
The questionnaire can be clicked with the round steering wheel button and head pose. 
The traffic lights are controlled through the interface on the server. 


### Analysis
Rerun .replay files are automatically stored on the server path: `C:\Users\<USERNAME>\AppData\LocalLow\<USERNAME>\XCDriving\test`

## Building for Testing

### Networked Standalone Oculus Quest
To demo Strangeland in standalone mode with the Oculus Quest, you must make a build and install it on your Oculus Quest. There are multiple substeps to get this working.
1. Set up Meta Quest Developer Hub (MQDH)
2. Make a build for android
    
Before making a build, you need to select several options in the Unity Editor. 

*  Edit > Project Settings > XR Plug-in Management > Android Settings > Select Initialize XR on startup and OpenXR as a Plug-in Provider
* Edit > Project Settings > Player > Scroll all the way down   > Check "Custom Main Manifest"
* Edit > Project Settings > XR Plug-in Management > OpenXR submenu > click the gear next to Meta Quest Support > Uncheck "Force Remove Internet Access"

3. Now, because of the networked structure of the simulator, you must have a device to act as your server and your meta quest 2 will be the client:
* run the program on your server device, select start as server
* make sure your meta quest is on the same network as your server device 

### Tethered Mode
make sure to download the Oculus app (so Unity knows that the system has an Oculus XR device on it) https://www.meta.com/help/quest/articles/headsets-and-accessories/oculus-rift-s/install-oculus-pc-app/
 Set up Meta Quest Developer Hub (MQDH)
Project Settings > XR Plug-in Management > Windows Settings > Select Initialize XR on startup and OpenXR as a Plug in Provider

As of 12/4/2023, the current architecture of the VR Driving Sim means that to use the Oculus Quest in strangeland in Tethered mode, you need to do some more setup. 

Make a Windows Build. (BuildSettings -> Select StandAlone -> Select SwitchPlatform -> Select Build)
Run this windows Build and click (Start As Server)
Start the Unity Editor and click (Join as Client A)


# License
Currently the project is not licensed for use. The scenes use assets from the Unity Asset Store under the [standard license](https://unity.com/legal/as-terms). 
