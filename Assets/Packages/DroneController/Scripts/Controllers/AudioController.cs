using UnityEngine;

namespace DroneController
{
    public class AudioController : MonoBehaviour
    {
        [Header("Local References:")]
        [SerializeField] private AudioSource _audioSource = default;
        [Header("Settings:")]
        [SerializeField] private float _volume = 0.1f;
        [SerializeField] private float _volumeVelocityMultiplier = 0.005f;
        [Space]
        [SerializeField] private float _pitch = 1f;
        [SerializeField] private float _pitchVelocityMultiplier = 0.0035f;

        private DroneMovement _droneMovement = default;

        private DroneMovement DroneMovement
        {
            get
            {
                if (_droneMovement == null)
                {
                    _droneMovement = GetComponent<DroneMovement>();
                }
                return _droneMovement;
            }
        }

        protected virtual void Update()
        {
            float calculatedVolume = _volume + (DroneMovement.Velocity.magnitude * _volumeVelocityMultiplier);
            float calculatedPitch = _pitch + (DroneMovement.Velocity.magnitude * _pitchVelocityMultiplier);
            _audioSource.volume = calculatedVolume;
            _audioSource.pitch = calculatedPitch;
        }
    }
}
