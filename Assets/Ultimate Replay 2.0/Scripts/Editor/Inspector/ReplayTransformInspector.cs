using UnityEditor;
using UnityEngine;

namespace UltimateReplay
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReplayTransform))]
    public class ReplayTransformInspector : ReplayRecordableBehaviourInspector
    {
        // Private
        private const float elementLabelWidth = 12;
        private const float multiElementLabelWidth = 30;
        private const float elementToggleWidth = 18;
        private const float elementControlSpacing = -5;

        private ReplayTransform targetTransform = null;
        private ReplayTransform[] targetTransforms = null;

        // Methods
        public override void OnEnable()
        {
            base.OnEnable();
            GetTargetInstances(out targetTransform, out targetTransforms);
        }
        
        public override void OnInspectorGUI()
        {
            DisplayDefaultInspectorProperties();

            DisplayPositionRotationScale();

            // Display storage info
            ReplayStorageStats.DisplayStorageStats(targetTransform);
        }

        public void DisplayPositionRotationScale()
        {
            // Get position values
            bool[] posX = GetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.X);
            bool[] posY = GetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.Y);
            bool[] posZ = GetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.Z);
            bool[] posLocal = GetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.Local);
            bool[] posInterpolate = GetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.Interoplate);
            bool[] posLowRes = GetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.LowRes);

            // Get rotation values
            bool[] rotX = GetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.X);
            bool[] rotY = GetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.Y);
            bool[] rotZ = GetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.Z);
            bool[] rotLocal = GetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.Local);
            bool[] rotIntepolate = GetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.Interoplate);
            bool[] rotLowRes = GetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.LowRes);

            // Get scale values
            bool[] scaX = GetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags.X);
            bool[] scaY = GetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags.Y);
            bool[] scaZ = GetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags.Z);
            bool[] scaInterpolate = GetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags.Interoplate);
            bool[] scaLowRes = GetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags.LowRes);
            bool[] dummy = null;


            // Display position field
            DisplayMultiElementField("Replay Position", false, ref posX, ref posY, ref posZ, ref posLocal, ref posInterpolate, ref posLowRes);
            DisplayMultiElementField("Replay Rotation", false, ref rotX, ref rotY, ref rotZ, ref rotLocal, ref rotIntepolate, ref rotLowRes);
            DisplayMultiElementField("Replay Scale", true, ref scaX, ref scaY, ref scaZ, ref dummy, ref scaInterpolate, ref scaLowRes);


            // Set flags
            SetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.X, posX);
            SetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.Y, posY);
            SetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.Z, posZ);
            SetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.Local, posLocal);
            SetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.Interoplate, posInterpolate);
            SetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags.LowRes, posLowRes);

            SetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.X, rotX);
            SetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.Y, rotY);
            SetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.Z, rotZ);
            SetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.Local, rotLocal);
            SetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.Interoplate, rotIntepolate);
            SetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags.LowRes, rotLowRes);

            SetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags.X, scaX);
            SetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags.Y, scaY);
            SetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags.Z, scaZ);
            SetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags.Interoplate, scaInterpolate);
            SetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags.LowRes, scaLowRes);

        }

        public void DisplayMultiElementField(string label, bool skipLocal, ref bool[] x, ref bool[] y, ref bool[] z, ref bool[] local, ref bool[] interpolate, ref bool[] lowRes)
        {
            // Check for narrow inspector window
            bool slimWidth = Screen.width < 375;
            bool individualElements = false;

            // Position
            GUILayout.BeginHorizontal();
            {
                // Label
                GUILayout.Label(label, GUILayout.Width(EditorGUIUtility.labelWidth));

                GUILayout.Space(-3);

                // XYZ
                if(AllValuesSet(x, true) == true && AllValuesSet(y, true) == true && AllValuesSet(z, true) == true)
                {
                    GUILayout.Label("XYZ", GUILayout.Width(multiElementLabelWidth));
                    GUILayout.Space(elementControlSpacing);
                    
                    bool recordXYZResult = GUILayout.Toggle(true, GUIContent.none, GUILayout.Width(elementToggleWidth));

                    // Check for untoggled
                    if (recordXYZResult == false)
                    {
                        // Disable all values
                        SetValues(ref x, false);
                        SetValues(ref y, false);
                        SetValues(ref z, false);

                        // Set dirty
                        foreach (UnityEngine.Object obj in targets)
                            EditorUtility.SetDirty(obj);
                    }
                }
                else
                {
                    individualElements = true;

                    // Display X
                    GUILayout.Label("X", GUILayout.Width(elementLabelWidth));
                    GUILayout.Space(elementControlSpacing);

                    bool changed = DisplayMultiEditableToggleOnly(ref x, GUILayout.Width(elementToggleWidth));

                    // Negative spacing
                    GUILayout.Space(-6);

                    // Display Y
                    GUILayout.Label("Y", GUILayout.Width(elementLabelWidth));
                    GUILayout.Space(elementControlSpacing);

                    changed |= DisplayMultiEditableToggleOnly(ref y, GUILayout.Width(elementToggleWidth));

                    // Negative spacing
                    GUILayout.Space(-6);

                    // Display Z
                    GUILayout.Label("Z", GUILayout.Width(elementLabelWidth));
                    GUILayout.Space(elementControlSpacing);

                    changed |= DisplayMultiEditableToggleOnly(ref z, GUILayout.Width(elementToggleWidth));

                    // Check for changed
                    if (changed == true)
                    {
                        foreach (UnityEngine.Object obj in targets)
                            EditorUtility.SetDirty(obj);
                    }
                }

                if (individualElements == true)
                {
                    // Small space
                    GUILayout.Space(3);
                }
                else
                {
                    GUILayout.Space(39);
                }


                if (skipLocal == false)
                {
                    GUILayout.Label(new GUIContent("Local", "Record local transformation or world transformation"), GUILayout.Width(34));
                    GUILayout.Space(elementControlSpacing);

                    bool changed = DisplayMultiEditableToggleOnly(ref local, GUILayout.Width(elementToggleWidth));

                    // Check for changed
                    if (changed == true)
                    {
                        foreach (UnityEngine.Object obj in targets)
                            EditorUtility.SetDirty(obj);
                    }
                }
                else
                {
                    GUILayout.Space(42 + elementControlSpacing + elementToggleWidth);
                }


                // Check for newline
                if (slimWidth == true)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUIUtility.labelWidth);
                }

                GUILayout.Space(1);

                // Interpolation
                GUILayout.Label(new GUIContent("Lerp", "Use interpolation during playback"), GUILayout.Width(30));
                GUILayout.Space(elementControlSpacing);

                bool valueChanged = DisplayMultiEditableToggleOnly(ref interpolate, GUILayout.Width(elementToggleWidth));


                if (slimWidth == true)
                {
                    GUILayout.Space(-9);
                    GUILayout.Label(new GUIContent("Low Precision", "Record the data in low precision. Not recommended for main game objects such as player"), GUILayout.Width(82));
                }
                else
                {
                    GUILayout.Label(new GUIContent("LP", "Record the data in low precision. Not recommended for main game objects such as player"), GUILayout.Width(18));
                }

                GUILayout.Space(elementControlSpacing);

               valueChanged = DisplayMultiEditableToggleOnly(ref lowRes, GUILayout.Width(elementToggleWidth));

                // Check for changed
                if (valueChanged == true)
                {
                    foreach (UnityEngine.Object obj in targets)
                        EditorUtility.SetDirty(obj);
                }
            }
            GUILayout.EndHorizontal();
        }

        private bool[] GetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags flag)
        {
            bool[] values = new bool[targetTransforms.Length];

            for (int i = 0; i < targetTransforms.Length; i++)
            {
                values[i] = (targetTransforms[i].positionFlags & flag) != 0;
            }

            return values;
        }

        private bool[] GetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags flag)
        {
            bool[] values = new bool[targetTransforms.Length];

            for (int i = 0; i < targetTransforms.Length; i++)
            {
                values[i] = (targetTransforms[i].rotationFlags & flag) != 0;
            }

            return values;
        }

        private bool[] GetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags flag)
        {
            bool[] values = new bool[targetTransforms.Length];

            for (int i = 0; i < targetTransforms.Length; i++)
            {
                values[i] = (targetTransforms[i].scaleFlags & flag) != 0;
            }

            return values;
        }

        private void SetFlagValuesForTargetsPosition(ReplayTransform.ReplayTransformFlags flag, bool[] toggleValues)
        {
            for (int i = 0; i < targetTransforms.Length; i++)
            {
                if (toggleValues[i] == true) targetTransforms[i].positionFlags |= flag;
                else targetTransforms[i].positionFlags &= ~flag;
            }
        }

        private void SetFlagValuesForTargetsRotation(ReplayTransform.ReplayTransformFlags flag, bool[] toggleValues)
        {
            for (int i = 0; i < targetTransforms.Length; i++)
            {
                if (toggleValues[i] == true) targetTransforms[i].rotationFlags |= flag;
                else targetTransforms[i].rotationFlags &= ~flag;
            }
        }

        private void SetFlagValuesForTargetsScale(ReplayTransform.ReplayTransformFlags flag, bool[] toggleValues)
        {
            for (int i = 0; i < targetTransforms.Length; i++)
            {
                if (toggleValues[i] == true) targetTransforms[i].scaleFlags |= flag;
                else targetTransforms[i].scaleFlags &= ~flag;
            }
        }

        private bool AllValuesSet(bool[] toggleValues, bool targetValue)
        {
            foreach (bool val in toggleValues)
                if (val != targetValue)
                    return false;

            return true;
        }

        private void SetValues(ref bool[] toggleValues, bool targetValue)
        {
            for (int i = 0; i < toggleValues.Length; i++)
                toggleValues[i] = targetValue;

        }
    }
}
