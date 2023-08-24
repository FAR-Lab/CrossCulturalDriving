using System.Collections;
using UnityEngine;

public class HeightOffsetter : MonoBehaviour {
    private void Start() {
        bodyTrackingManager = FindObjectOfType<ZEDBodyTrackingManager>();
        if (bodyTrackingManager == null) Debug.LogError("ZEDManagerIK: No body tracking manager loaded!");
        lastCallTime = Time.time;
    }

    /// <summary>
    ///     Find an offset that sets both feet above ground, and at least one foot on the ground.
    /// </summary>
    /// <returns>Offset to apply to avatar to stick it to the ground. Offset should be added, not subtracted.</returns>
    public Vector3 ComputeRootHeightOffsetFromRaycastInfo(Vector3 posAnkleL, Vector3 posAnkleR, Vector3 hitPointL,
        Vector3 hitPointR, float ankleHeightOffset) {
        if (bodyTrackingManager.automaticOffset) {
            // Oriented distances from soles (ankle + offset) to virtual object below or above
            // Positive : Foot is under ground. Negative: Foot is above ground.
            var offsetToApplyL = hitPointL.y - posAnkleL.y + ankleHeightOffset;
            var offsetToApplyR = hitPointR.y - posAnkleR.y + ankleHeightOffset;

            // If 1 foot at least is within search for floor range
            if (Mathf.Abs(offsetToApplyL) < findFloorDistance || Mathf.Abs(offsetToApplyR) < findFloorDistance) {
                // Check if lowest foot is close enough to the floor to reset the timer
                if (LowestFootIsCloseToGround(offsetToApplyL, offsetToApplyR)) {
                    durationOffsetError = 0f;
                    return new Vector3(0, currentAutoHeightOffset, 0);
                }

                var time = Time.time;
                durationOffsetError += time - lastCallTime;
                lastCallTime = time;

                if (durationOffsetError > thresholdDurationOffsetError) {
                    var startAutoHeightOffset = currentAutoHeightOffset;
                    RecalculateOffset(offsetToApplyL, offsetToApplyR, out var targetAutoHeightOffset);
                    StartCoroutine(LerpAutoOffset(.5f, startAutoHeightOffset, targetAutoHeightOffset));
                    durationOffsetError = 0f;
                    return new Vector3(0, currentAutoHeightOffset, 0);
                }

                // error estimation in progress, don't change offset
                return new Vector3(0, currentAutoHeightOffset, 0);
            }

            // both feet too far: don't change current offset
            return new Vector3(0, currentAutoHeightOffset, 0);
        }

        // Automatic offset disabled
        return bodyTrackingManager.manualOffset;
    }

    /// <summary>
    ///     Check if the lowest foot is close enough to ground to reset the timer.
    /// </summary>
    /// <returns>False if lowest foot is too far from the virtual floor. True else.</returns>
    private bool LowestFootIsCloseToGround(float offsetL, float offsetR) {
        var b = offsetL <= offsetR
            ? Mathf.Abs(offsetL) <= thresholdDistCloseToFloor
            : Mathf.Abs(offsetR) <= thresholdDistCloseToFloor;
        return b;
    }

    private void RecalculateOffset(float offsetToApplyL, float offsetToApplyR, out float targetAutoHeightOffset) {
        targetAutoHeightOffset = Mathf.Max(offsetToApplyL, offsetToApplyR);
    }

    private IEnumerator LerpAutoOffset(float timeToLerp, float startValue, float targetValue) {
        if (timeToLerp <= 0) {
            currentAutoHeightOffset = targetValue;
            yield break;
        }

        float lerptime = 0;
        float lerpVal;

        while (lerptime < timeToLerp) {
            lerpVal = lerptime / timeToLerp;
            currentAutoHeightOffset = Mathf.Lerp(startValue, targetValue, lerpVal);
            lerptime += Time.deltaTime;
            yield return 0;
        }

        durationOffsetError = 0f;
    }

    #region vars

    [Header("Main settings")] private ZEDBodyTrackingManager bodyTrackingManager;

    [SerializeField] private float currentAutoHeightOffset;

    public float CurrentheightOffset {
        get => currentAutoHeightOffset;
        set => currentAutoHeightOffset = value;
    }

    [Header("Finding the ground")]
    [Tooltip("Max height difference between feet and floor that will trigger the application of the offset." +
             "\nIf the floor is further than this value, above or under, the height will not be offset.")]
    public float findFloorDistance = 2f;

    [Tooltip("Which layers to use when searching the floor above or under.")]
    public LayerMask layersToHit;

    // Threshold to detect height offset error, in meters.
    [SerializeField] private float thresholdDistCloseToFloor = 0.02f;

    // Duration of successive height offset error frames that should trigger a reset of the auto offset, in seconds.
    [SerializeField] private float thresholdDurationOffsetError = 3f;
    private float durationOffsetError;
    private float lastCallTime;

    #endregion
}