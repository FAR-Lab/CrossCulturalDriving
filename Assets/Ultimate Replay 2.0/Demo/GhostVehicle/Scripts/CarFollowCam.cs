using UnityEngine;

namespace UltimateReplay.Example
{
    /// <summary>
    /// A simple follow camera script used in the ghost vehicle demo.
    /// </summary>
    public class CarFollowCam : MonoBehaviour
    {
        // Public
        /// <summary>
        /// The transform to follow.
        /// </summary>
        public Transform target;
        /// <summary>
        /// The camera follow height value.
        /// </summary>
        public float height = 3;
        /// <summary>
        /// The camera follow distance value.
        /// </summary>
        public float distance = 3;
        /// <summary>
        /// The speed that the camera moves towards its target view position.
        /// </summary>
        public float speed = 5;
        /// <summary>
        /// The look height for the target object to control the up/down angle of the camera to frame the target object correctly.
        /// </summary>
        public float lookHeight = 1;

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Update()
        {
            Vector3 wantedPosition;
            //if (followBehind)
                wantedPosition = target.TransformPoint(0, height, -distance);
            //else
            //    wantedPosition = target.TransformPoint(0, height, distance);

            transform.position = Vector3.Lerp(transform.position, wantedPosition, Time.deltaTime * speed);

            //if (smoothRotation)
            //{
            //    Quaternion wantedRotation = Quaternion.LookRotation(target.position - transform.position, target.up);
            //    transform.rotation = Quaternion.Slerp(transform.rotation, wantedRotation, Time.deltaTime * rotationDamping);
            //}
            //else 
                transform.LookAt(target, target.up);
        }
    }
}
