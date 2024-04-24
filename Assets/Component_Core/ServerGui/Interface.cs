/* Flashing button example */

using UnityEngine;

public class Interface : MonoBehaviour {
    public Texture2D onIcon;
    public Texture2D offIcon;
    private readonly bool[] assigned = { false, false, false, false, false, false };
    private string button_name;
    private int connected_devices;
    private int dev1 = -1;
    private int dev2 = -1;
    private readonly int num_of_devices = 2;
    private readonly int num_of_participant = 6;
    private readonly string[] participant = { "P1", "P2", "P3", "P4", "P5", "P6" };

    private void OnGUI() {
        for (var i = 0; i < num_of_participant; i++) {
            GUI.Box(new Rect(10 + i * 100, 10, 100, 140), participant[i]);
            // Make the assign button.
            button_name = (i == dev1) | (i == dev2) ? "Dissociate" : "Assign";
            if (GUI.Button(new Rect(20 + i * 100, 70, 80, 20), button_name)) {
                Debug.Log("you clicked " + participant[i]);
                if (connected_devices < num_of_devices && !assigned[i]) {
                    // Assign new devices to participant.
                    assigned[i] = !assigned[i];
                    connected_devices++;

                    if (dev1 == -1)
                        dev1 = i;
                    else
                        dev2 = i;
                }
                else if (assigned[i]) {
                    // Disconnect device.
                    assigned[i] = !assigned[i];
                    connected_devices--;
                    if (dev1 == i)
                        dev1 = -1;
                    else
                        dev2 = -1;
                }
                else {
                    // No available device for new participant.
                    Debug.Log("No device Available");
                }
            }

            if (GUI.Button(new Rect(20 + i * 100, 120, 80, 20), "Calibrate"))
                Debug.Log("Calibrate Participant: " + (i + 1));

            if (assigned[i])
                GUI.Box(new Rect(45 + i * 100, 40, 20, 20), onIcon);
            else
                GUI.Box(new Rect(45 + i * 100, 40, 20, 20), offIcon);
            GUI.Label(new Rect(40 + dev1 * 100, 90, 80, 20), "No. 1");
            GUI.Label(new Rect(40 + dev2 * 100, 90, 80, 20), "No. 2");
        }
    }
}