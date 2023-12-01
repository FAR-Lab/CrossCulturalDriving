using System.Collections;
using UnityEngine;

namespace DroneController
{
    public class SparkManager : MonoBehaviour
    {
        public static SparkManager Instance;

        [Header("Project References:")]
        [SerializeField] private GameObject _prefabCollisionSpark = default;
        [Header("Settings:")]
        [SerializeField] private float _sparkSize = 2f;
        [SerializeField] private float _keepSparkAliveTime = 1f;

        private void Awake()
        {
            if (SparkManager.Instance == null)
            {
                Instance = this;
            }
            else if (SparkManager.Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            SparkCollisionDetection.CollisionDetected += OnCollisionDetected;
        }

        private void OnDisable()
        {
            SparkCollisionDetection.CollisionDetected -= OnCollisionDetected;
        }

        private void OnCollisionDetected(ContactPoint contactPoint)
        {
            if (!_prefabCollisionSpark)
            {
                // Missing prefab reference.
                return;
            }

            // Calculate rotation and position values.
            Quaternion sparkRotation = Quaternion.FromToRotation(Vector3.up, contactPoint.normal) * Quaternion.Euler(-90, 0, 0);
            Vector3 sparkPosition = contactPoint.point;

            // Instantiate the spark object.
            GameObject spark = (GameObject)Instantiate(_prefabCollisionSpark, sparkPosition, sparkRotation, transform);

            // Set correct scale.
            spark.transform.localScale = transform.localScale * _sparkSize;
            foreach (Transform _spark in spark.transform)
            {
                _spark.localScale = transform.localScale * _sparkSize;
            }

            StartCoroutine(SparksCleaner(spark));
        }

        private IEnumerator SparksCleaner(GameObject spark)
        {
            yield return new WaitForSeconds(_keepSparkAliveTime);
            Destroy(spark);
        }
    }
}
