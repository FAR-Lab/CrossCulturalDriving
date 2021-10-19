using System.Collections;
using UnityEngine;

namespace UltimateReplay.Example
{
    /// <summary>
    /// An example script that shows how a meshod call can be recorded and replayed similar to a networked RPC call.
    /// </summary>
    public class Ex_RecordMethod : ReplayBehaviour
    {
        // Methods
        public IEnumerator Start()
        {
            while(true)
            {
                yield return new WaitForSeconds(1f);

                // Only record methods when this object is recording
                if (IsRecording == true)
                {
                    // This will cause the method to be called immediatley and also at the corrosponding time during playback
                    RecordMethodCall(MyExampleMethod, "Hello World");
                }
            }
        }

        [ReplayMethod]
        public void MyExampleMethod(string message)
        {
            Debug.Log("This is the message: " + message);
        }
    }
}
