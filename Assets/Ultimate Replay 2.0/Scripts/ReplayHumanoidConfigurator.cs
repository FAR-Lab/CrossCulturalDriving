using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// Util class used to setup and configure humanoid characters in order to record animation, IK and ragdolls.
    /// This setup process requires a valid humanoid animator to be setup on the target object.
    /// </summary>
    public class ReplayHumanoidConfigurator
    {
        // Types
        /// <summary>
        /// Configuration data for the transform components that will be attached to the humanoid hierarchy.
        /// </summary>
        public sealed class ReplayTransformConfiguration
        {
            // Public
            public bool attachReplayTransformComponent = true;
            public ReplayTransform.ReplayTransformFlags boneTransformPositionFlags = 0;
            public ReplayTransform.ReplayTransformFlags boneTransformRotationFlags = 0;
            public ReplayTransform.ReplayTransformFlags boneTransformScaleFlags = 0;

            public static readonly ReplayTransformConfiguration defaultRoot = new ReplayTransformConfiguration(false);
            public static readonly ReplayTransformConfiguration defaultLocal = new ReplayTransformConfiguration(true);

            // Constructor
            public ReplayTransformConfiguration() { }

            private ReplayTransformConfiguration(bool local)
            {
                if(local == true)
                {
                    boneTransformPositionFlags = ReplayTransform.ReplayTransformFlags.XYZ_Local;
                    boneTransformRotationFlags = ReplayTransform.ReplayTransformFlags.XYZ_Local;
                    boneTransformScaleFlags = ReplayTransform.ReplayTransformFlags.None;
                }
                else
                {
                    boneTransformPositionFlags = ReplayTransform.ReplayTransformFlags.XYZ_World;
                    boneTransformRotationFlags = ReplayTransform.ReplayTransformFlags.XYZ_World;
                    boneTransformScaleFlags = ReplayTransform.ReplayTransformFlags.None;
                }
            }

            // Methods
            public static ReplayTransformConfiguration FromReplayTransform(ReplayTransform transform, bool attachReplayTransformComponent)
            {
                ReplayTransformConfiguration result = new ReplayTransformConfiguration();

                result.boneTransformPositionFlags = transform.positionFlags;
                result.boneTransformRotationFlags = transform.rotationFlags;
                result.boneTransformScaleFlags = transform.scaleFlags;
                result.attachReplayTransformComponent = attachReplayTransformComponent;

                return result;
            }
        }

        /// <summary>
        /// Result object returned by the configurator which contains information about all replay componen5ts that were added.
        /// </summary>
        public struct ReplayHumanoidConfigurationResult
        {
            // Public
            public Transform rootTransform;
            public ReplayObject rootReplayObject;
            public ReplayTransform rootReplayTransform;

            public ReplayBoneConfigurationResult[] configuredBones;
        }

        /// <summary>
        /// Contains information about a specific bone in the humanoid hierarchy including the attached <see cref="ReplayTransform"/> that was added during the process.
        /// </summary>
        public struct ReplayBoneConfigurationResult
        {
            // Public
            public Transform boneTransform;
            public ReplayTransform boneReplayTransform;
        }

        // Public
        public static readonly HumanBodyBones[] allBones = (HumanBodyBones[])Enum.GetValues(typeof(HumanBodyBones));

        // Methods
        public static ReplayHumanoidConfigurationResult ConfigureHumanoidObjectFromAnimator(GameObject objectRoot, ReplayTransformConfiguration rootTransformConfiguration = null, ReplayTransformConfiguration boneTransformConfiguration = null, HumanBodyBones[] setupBones = null)
        {
            // Check for null
            if (objectRoot == null) throw new ArgumentNullException("objectRoot");

            // Get default configurations
            if (rootTransformConfiguration == null) rootTransformConfiguration = ReplayTransformConfiguration.defaultRoot;
            if (boneTransformConfiguration == null) boneTransformConfiguration = ReplayTransformConfiguration.defaultLocal;

            // Get default bones
            if (setupBones == null) setupBones = allBones;

            // Check for animator
            Animator anim = objectRoot.GetComponent<Animator>();

            if (anim == null)
                throw new NotSupportedException("A humanoid can only be configured if an Animator component is attached");
            
            // Setup root
            ReplayObject rootReplayObject = objectRoot.GetComponent<ReplayObject>();
            ReplayTransform rootReplayTransform = objectRoot.GetComponent<ReplayTransform>();

            // Add the replay object component
            if (rootReplayObject == null)
                rootReplayObject = objectRoot.AddComponent<ReplayObject>();

            // Add the root transform component
            if (rootReplayTransform == null && rootTransformConfiguration.attachReplayTransformComponent == true)
                rootReplayTransform = objectRoot.AddComponent<ReplayTransform>();


            // Setup transform
            if(rootReplayTransform != null)
            {
                rootReplayTransform.positionFlags = rootTransformConfiguration.boneTransformPositionFlags;
                rootReplayTransform.rotationFlags = rootTransformConfiguration.boneTransformRotationFlags;
                rootReplayTransform.scaleFlags = rootTransformConfiguration.boneTransformScaleFlags;
            }


            List<ReplayBoneConfigurationResult> configuredBones = new List<ReplayBoneConfigurationResult>();

            // Get all bones
            foreach(HumanBodyBones bone in setupBones)
            {
                // Try to get the matching transform
                Transform boneTransform = anim.GetBoneTransform(bone);

                // Check for found bone
                if (boneTransform != null)
                {
                    // COnfigure the bone
                    ReplayBoneConfigurationResult boneSetup = ConfigueHumanoidBoneObject(boneTransform.gameObject, boneTransformConfiguration);

                    // Add to result
                    configuredBones.Add(boneSetup);
                }
            }

            // Update the replay object
            rootReplayObject.RebuildComponentList();

            // Create result
            return new ReplayHumanoidConfigurationResult
            {
                rootTransform = objectRoot.transform,
                rootReplayObject = rootReplayObject,
                rootReplayTransform = rootReplayTransform,

                configuredBones = configuredBones.ToArray(),
            };
        }

        public static ReplayBoneConfigurationResult ConfigueHumanoidBoneObject(GameObject objectBone, ReplayTransformConfiguration transformConfiguration = null)
        {
            // Check for null
            if (objectBone == null) throw new ArgumentNullException("objectBone");

            // Get default configurations
            if (transformConfiguration == null) transformConfiguration = ReplayTransformConfiguration.defaultLocal;


            // Setup bone
            ReplayTransform boneReplayTransform = objectBone.GetComponent<ReplayTransform>();

            // Add the replay transform component
            if (transformConfiguration.attachReplayTransformComponent == true && boneReplayTransform == null)
                boneReplayTransform = objectBone.AddComponent<ReplayTransform>();


            // Setup transform
            if (boneReplayTransform != null)
            {
                boneReplayTransform.positionFlags = transformConfiguration.boneTransformPositionFlags;
                boneReplayTransform.rotationFlags = transformConfiguration.boneTransformRotationFlags;
                boneReplayTransform.scaleFlags = transformConfiguration.boneTransformScaleFlags;
            }

            // Create the result
            return new ReplayBoneConfigurationResult
            {
                boneTransform = objectBone.transform,
                boneReplayTransform = boneReplayTransform,
            };
        }
    }
}
