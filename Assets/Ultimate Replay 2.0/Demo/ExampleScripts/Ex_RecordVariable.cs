using System.Collections;
using UnityEngine;

namespace UltimateReplay.Example
{
    /// <summary>
    /// Example script that shows how a field variable can be recorded and replayed
    /// </summary>
    public class Ex_RecordVariable : ReplayBehaviour // Inherit from 'ReplayBehaviour' is important
    {
        // Public
        // Declare a field variable with the 'ReplayVar' in order to record its value
        [ReplayVar]
        public int recordVariable = 100;

        // Methods
        public void Update()
        {
            if(IsRecording == true)
            {
                // Increase the value during recording so that we have something interesting to replay
                recordVariable++;
            }

            if(IsReplaying == true)
            {
                // Ouput the replayed value to the console
                Debug.Log("Recorded Value: " + recordVariable);
            }
        }
    }
}
