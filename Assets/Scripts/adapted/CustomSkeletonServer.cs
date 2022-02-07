/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HandDataStreamerReader))]
public class CustomSkeletonServer : MonoBehaviour {
    private HandDataStreamerReader _dataProvider;


    public struct SkeletonPoseData {
        public OVRPlugin.Posef RootPose { get; set; }
        public float RootScale { get; set; }
        public OVRPlugin.Quatf[] BoneRotations { get; set; }
        public bool IsDataValid { get; set; }
        public bool IsDataHighConfidence { get; set; }
        public int SkeletonChangedCount { get; set; }
    }


    [SerializeField] protected OVRPlugin.SkeletonType _skeletonType = OVRPlugin.SkeletonType.None;

    [SerializeField] private bool _updateRootPose = false;
    [SerializeField] private bool _updateRootScale = false;

    private GameObject _bonesGO;
    private GameObject _bindPosesGO;

    protected List<OVRBone> _bones;
    private List<OVRBone> _bindPoses;

    private readonly Quaternion wristFixupRotation = new Quaternion(0.0f, 1.0f, 0.0f, 0.0f);

    public bool IsInitialized { get; private set; }
    public bool IsDataValid { get; private set; }
    public bool IsDataHighConfidence { get; private set; }
    public IList<OVRBone> Bones { get; protected set; }
    public IList<OVRBone> BindPoses { get; private set; }
    public IList<OVRBoneCapsule> Capsules { get; private set; }
    public OVRPlugin.SkeletonType GetSkeletonType() { return _skeletonType; }
    public int SkeletonChangedCount { get; private set; }

    private void Awake() {
        if (_dataProvider == null) { _dataProvider = GetComponent<HandDataStreamerReader>(); }

        _bones = new List<OVRBone>();
        Bones = _bones.AsReadOnly();

        _bindPoses = new List<OVRBone>();
        BindPoses = _bindPoses.AsReadOnly();
    }

    private void Start() {
        if (ShouldInitialize()) { Initialize(); }
    }

    private bool ShouldInitialize() {
        if (IsInitialized) { return false; }

        if (_skeletonType == OVRPlugin.SkeletonType.None) { return false; }
        else if (_skeletonType == OVRPlugin.SkeletonType.HandLeft ||
                 _skeletonType == OVRPlugin.SkeletonType.HandRight) { return true; }
        else { return true; }
    }

    protected OVRPlugin.Skeleton2 _skeleton = new OVRPlugin.Skeleton2();

    [HideInInspector] [SerializeField]
    private List<Transform> _customBones_V2 = new List<Transform>(new Transform[(int) OVRPlugin.BoneId.Max]);

#if UNITY_EDITOR

    private static readonly string[] _fbxHandSidePrefix = {"l_", "r_"};
    private static readonly string _fbxHandBonePrefix = "b_";

    private static readonly string[] _fbxHandBoneNames = {
        "wrist",
        "forearm_stub",
        "thumb0",
        "thumb1",
        "thumb2",
        "thumb3",
        "index1",
        "index2",
        "index3",
        "middle1",
        "middle2",
        "middle3",
        "ring1",
        "ring2",
        "ring3",
        "pinky0",
        "pinky1",
        "pinky2",
        "pinky3"
    };

    private static readonly string[] _fbxHandFingerNames = {
        "thumb",
        "index",
        "middle",
        "ring",
        "pinky"
    };


#endif
    private bool _applyBoneTranslations = true;
    public List<Transform> CustomBones { get { return _customBones_V2; } }

#if UNITY_EDITOR
    public void TryAutoMapBonesByName() {
        OVRPlugin.BoneId start = (OVRPlugin.BoneId) GetCurrentStartBoneId();
        OVRPlugin.BoneId end = (OVRPlugin.BoneId) GetCurrentEndBoneId();
        OVRPlugin.SkeletonType skeletonType = GetSkeletonType();
        if (start != OVRPlugin.BoneId.Invalid && end != OVRPlugin.BoneId.Invalid) {
            for (int bi = (int) start; bi < (int) end; ++bi) {
                string fbxBoneName = FbxBoneNameFromBoneId(skeletonType, (OVRPlugin.BoneId) bi);
                Transform t = transform.FindChildRecursive(fbxBoneName);

                if (t != null) { _customBones_V2[(int) bi] = t; }
            }
        }
    }

    private static string FbxBoneNameFromBoneId(OVRPlugin.SkeletonType skeletonType, OVRPlugin.BoneId bi) {
        {
            if (bi >= OVRPlugin.BoneId.Hand_ThumbTip && bi <= OVRPlugin.BoneId.Hand_PinkyTip) {
                return _fbxHandSidePrefix[(int) skeletonType] +
                       _fbxHandFingerNames[(int) bi - (int) OVRPlugin.BoneId.Hand_ThumbTip] + "_finger_tip_marker";
            }
            else { return _fbxHandBonePrefix + _fbxHandSidePrefix[(int) skeletonType] + _fbxHandBoneNames[(int) bi]; }
        }
    }
#endif

    private void InitializeBones() {
        bool flipX = (_skeletonType == OVRPlugin.SkeletonType.HandLeft ||
                      _skeletonType == OVRPlugin.SkeletonType.HandRight);

        // if (_bones == null || _bones.Count != (int) OVRPlugin.BoneId.Max) {
        if (_bones == null || _bones.Count != _skeleton.NumBones) {
            _bones = new List<OVRBone>(new OVRBone[_skeleton.NumBones]);
            Bones = _bones.AsReadOnly();
        }

        for (int i = 0; i < _bones.Count; ++i) {
            OVRBone bone = _bones[i] ?? (_bones[i] = new OVRBone());
            bone.Id = (OVRSkeleton.BoneId) _skeleton.Bones[i].Id;
            bone.ParentBoneIndex = _skeleton.Bones[i].ParentBoneIndex;
            bone.Transform = _customBones_V2[(int) bone.Id];
            if (_applyBoneTranslations) {
                bone.Transform.localPosition = flipX
                    ? _skeleton.Bones[i].Pose.Position.FromFlippedXVector3f()
                    : _skeleton.Bones[i].Pose.Position.FromFlippedZVector3f();
            }

            bone.Transform.localRotation = flipX
                ? _skeleton.Bones[i].Pose.Orientation.FromFlippedXQuatf()
                : _skeleton.Bones[i].Pose.Orientation.FromFlippedZQuatf();
        }
    }


    private void Initialize() {
        if (OverrideGetSkeleton2(_skeletonType)) {
            InitializeBones();
            InitializeBindPose();
            IsInitialized = true;
        }
    }


    private bool OverrideGetSkeleton2(OVRPlugin.SkeletonType skeletonType) {
        //  OVRPlugin.GetSkeleton2((OVRPlugin.SkeletonType) _skeletonType, ref _skeleton);
        if (skeletonType == OVRPlugin.SkeletonType.HandLeft) {
            _skeleton.NumBones = 24;
            _skeleton.NumBoneCapsules = 0;
            _skeleton.Type = OVRPlugin.SkeletonType.HandLeft;
            _skeleton.Bones = new OVRPlugin.Bone[_skeleton.NumBones];
            _skeleton.Bones[0] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Start, ParentBoneIndex = -1,
                Pose = new OVRPlugin.Posef()
                    {Position = new Vector3(0, 0, 0).ToVector3f(), Orientation = new Quaternion(0, 0, 0, 1).ToQuatf()}
            };
            _skeleton.Bones[1] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_ForearmStub, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef()
                    {Position = new Vector3(0, 0, 0).ToVector3f(), Orientation = new Quaternion(0, 0, 0, 1).ToQuatf()}
            };
            _skeleton.Bones[2] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Thumb0, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.0200693f, 0.0115541f, -0.01049652f).ToVector3f(),
                    Orientation = new Quaternion(0.3753869f, 0.4245841f, -0.007778856f, 0.8238644f).ToQuatf()
                }
            };
            _skeleton.Bones[3] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Thumb1, ParentBoneIndex = 2,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.02485256f, -9.31E-10f, -1.863E-09f).ToVector3f(),
                    Orientation = new Quaternion(0.2602303f, 0.02433088f, 0.125678f, 0.9570231f).ToQuatf()
                }
            };
            _skeleton.Bones[4] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Thumb2, ParentBoneIndex = 3,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.03251291f, 5.82E-10f, 1.863E-09f).ToVector3f(),
                    Orientation = new Quaternion(-0.08270377f, -0.0769617f, -0.08406223f, 0.9900357f).ToQuatf()
                }
            };

            _skeleton.Bones[5] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Thumb3, ParentBoneIndex = 4,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.0337931f, 3.26E-09f, 1.863E-09f).ToVector3f(),
                    Orientation = new Quaternion(0.08350593f, 0.06501573f, -0.05827406f, 0.992675f).ToQuatf()
                }
            };

            _skeleton.Bones[6] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Index1, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.09599624f, 0.007316455f, -0.02355068f).ToVector3f(),
                    Orientation = new Quaternion(0.03068309f, -0.01885559f, 0.04328144f, 0.9984136f).ToQuatf()
                }
            };

            _skeleton.Bones[7] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Index2, ParentBoneIndex = 6,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.0379273f, -5.82E-10f, -5.97E-10f).ToVector3f(),
                    Orientation = new Quaternion(-0.02585241f, -0.007116061f, 0.003292944f, 0.999635f).ToQuatf()
                }
            };

            _skeleton.Bones[8] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Index3, ParentBoneIndex = 7,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.02430365f, -6.73E-10f, -6.75E-10f).ToVector3f(),
                    Orientation = new Quaternion(-0.016056f, -0.02714872f, -0.072034f, 0.9969034f).ToQuatf()
                }
            };

            _skeleton.Bones[9] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Middle1, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.09564661f, 0.002543155f, -0.001725906f).ToVector3f(),
                    Orientation = new Quaternion(-0.009066326f, -0.05146559f, 0.05183575f, 0.9972874f).ToQuatf()
                }
            };

            _skeleton.Bones[10] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Middle2, ParentBoneIndex = 9,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.042927f, -8.51E-10f, -1.193E-09f).ToVector3f(),
                    Orientation = new Quaternion(-0.01122823f, -0.004378874f, -0.001978267f, 0.9999254f).ToQuatf()
                }
            };

            _skeleton.Bones[11] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Middle3, ParentBoneIndex = 10,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.02754958f, 3.09E-10f, 1.128E-09f).ToVector3f(),
                    Orientation = new Quaternion(-0.03431955f, -0.004611839f, -0.09300701f, 0.9950631f).ToQuatf()
                }
            };

            _skeleton.Bones[12] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Ring1, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.0886938f, 0.006529308f, 0.01746524f).ToVector3f(),
                    Orientation = new Quaternion(-0.05315936f, -0.1231034f, 0.04981349f, 0.9897162f).ToQuatf()
                }
            };

            _skeleton.Bones[13] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Ring2, ParentBoneIndex = 12,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.0389961f, 0f, 5.24E-10f).ToVector3f(),
                    Orientation = new Quaternion(-0.03363252f, -0.00278984f, 0.00567602f, 0.9994143f).ToQuatf()
                }
            };

            _skeleton.Bones[14] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Ring3, ParentBoneIndex = 13,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.02657339f, 1.281E-09f, 1.63E-09f).ToVector3f(),
                    Orientation = new Quaternion(-0.003477462f, 0.02917945f, -0.02502854f, 0.9992548f).ToQuatf()
                }
            };
            _skeleton.Bones[15] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Pinky0, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.03407356f, 0.009419836f, 0.02299858f).ToVector3f(),
                    Orientation = new Quaternion(-0.207036f, -0.1403428f, 0.0183118f, 0.9680417f).ToQuatf()
                }
            };

            _skeleton.Bones[16] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Pinky1, ParentBoneIndex = 15,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.04565055f, 9.97679E-07f, -2.193963E-06f).ToVector3f(),
                    Orientation = new Quaternion(0.09111304f, 0.00407137f, 0.02812923f, 0.9954349f).ToQuatf()
                }
            };

            _skeleton.Bones[17] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Pinky2, ParentBoneIndex = 16,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.03072042f, 1.048E-09f, -1.75E-10f).ToVector3f(),
                    Orientation = new Quaternion(-0.03761665f, -0.04293772f, -0.01328605f, 0.9982809f).ToQuatf()
                }
            };

            _skeleton.Bones[18] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Pinky3, ParentBoneIndex = 17,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.02031138f, -2.91E-10f, 9.31E-10f).ToVector3f(),
                    Orientation = new Quaternion(0.0006447434f, 0.04917067f, -0.02401883f, 0.9985014f).ToQuatf()
                }
            };

            _skeleton.Bones[19] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_MaxSkinnable, ParentBoneIndex = 5,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.02459077f, -0.001026974f, 0.0006703701f).ToVector3f(),
                    Orientation = new Quaternion(0f, 0f, 0f, 1f).ToQuatf()
                }
            };

            _skeleton.Bones[20] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_IndexTip, ParentBoneIndex = 8,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.02236338f, -0.00102507f, 0.0002956076f).ToVector3f(),
                    Orientation = new Quaternion(0f, 0f, 0f, 1f).ToQuatf()
                }
            };

            _skeleton.Bones[21] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_MiddleTip, ParentBoneIndex = 11,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.02496492f, -0.001137299f, 0.0003086528f).ToVector3f(),
                    Orientation = new Quaternion(0f, 0f, 0f, 1f).ToQuatf()
                }
            };

            _skeleton.Bones[22] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_RingTip, ParentBoneIndex = 14,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.02432613f, -0.001608172f, 0.000257905f).ToVector3f(),
                    Orientation = new Quaternion(0f, 0f, 0f, 1f).ToQuatf()
                }
            };

            _skeleton.Bones[23] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_PinkyTip, ParentBoneIndex = 18,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(0.02192238f, -0.001216086f, -0.0002464796f).ToVector3f(),
                    Orientation = new Quaternion(0f, 0f, 0f, 1f).ToQuatf()
                }
            };
        }

        else if (skeletonType == OVRPlugin.SkeletonType.HandRight) {
            _skeleton.NumBones = 24;
            _skeleton.NumBoneCapsules = 0;
            _skeleton.Type = OVRPlugin.SkeletonType.HandRight;
            _skeleton.Bones = new OVRPlugin.Bone[_skeleton.NumBones];
            _skeleton.Bones[0] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Start, ParentBoneIndex = -1,
                Pose = new OVRPlugin.Posef()
                    {Position = new Vector3(0, 0, 0).ToVector3f(), Orientation = new Quaternion(0, 0, 0, 1).ToQuatf()}
            };
            _skeleton.Bones[1] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_ForearmStub, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef()
                    {Position = new Vector3(0, 0, 0).ToVector3f(), Orientation = new Quaternion(0, 0, 0, 1).ToQuatf()}
            };
            _skeleton.Bones[2] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Thumb0, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.0200693f, -0.0115541f, 0.01049652f).ToVector3f(),
                    Orientation = new Quaternion(0.3753869f, 0.4245841f, -0.007778856f, 0.8238644f).ToQuatf()
                }
            };
            _skeleton.Bones[3] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Thumb1, ParentBoneIndex = 2,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.02485256f, 2.328E-09f, 0f).ToVector3f(),
                    Orientation = new Quaternion(0.2602303f, 0.02433088f, 0.125678f, 0.9570231f).ToQuatf()
                }
            };
            _skeleton.Bones[4] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Thumb2, ParentBoneIndex = 3,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.03251291f, -1.16E-10f, 0f).ToVector3f(),
                    Orientation = new Quaternion(-0.08270377f, -0.0769617f, -0.08406223f, 0.9900357f).ToQuatf()
                }
            };
            _skeleton.Bones[5] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Thumb3, ParentBoneIndex = 4,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.0337931f, -3.26E-09f, -1.863E-09f).ToVector3f(),
                    Orientation = new Quaternion(0.08350593f, 0.06501573f, -0.05827406f, 0.9926752f).ToQuatf()
                }
            };

            _skeleton.Bones[6] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Index1, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.09599624f, -0.007316455f, 0.02355068f).ToVector3f(),
                    Orientation = new Quaternion(0.03068309f, -0.01885559f, 0.04328144f, 0.9984136f).ToQuatf()
                }
            };

            _skeleton.Bones[7] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Index2, ParentBoneIndex = 6,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.0379273f, 1.16E-10f, 5.97E-10f).ToVector3f(),
                    Orientation = new Quaternion(-0.02585241f, -0.007116061f, 0.003292944f, 0.999635f).ToQuatf()
                }
            };

            _skeleton.Bones[8] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Index3, ParentBoneIndex = 7,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.02430365f, 6.73E-10f, 6.75E-10f).ToVector3f(),
                    Orientation = new Quaternion(-0.016056f, -0.02714872f, -0.072034f, 0.99690344f).ToQuatf()
                }
            };

            _skeleton.Bones[9] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Middle1, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.09564661f, -0.002543155f, 0.00172590f).ToVector3f(),
                    Orientation = new Quaternion(-0.009066326f, -0.05146559f, 0.05183575f, 0.9972874f).ToQuatf()
                }
            };

            _skeleton.Bones[10] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Middle2, ParentBoneIndex = 9,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.042927f, 1.317E-09f, 1.193E-09f).ToVector3f(),
                    Orientation = new Quaternion(-0.01122823f, -0.004378874f, -0.001978267f, 0.9999254f).ToQuatf()
                }
            };

            _skeleton.Bones[11] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Middle3, ParentBoneIndex = 10,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.02754958f, -7.71E-10f, -1.12E-09f).ToVector3f(),
                    Orientation = new Quaternion(-0.03431955f, -0.004611839f, -0.09300701f, 0.9950631f).ToQuatf()
                }
            };

            _skeleton.Bones[12] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Ring1, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.0886938f, -0.006529307f, -0.01746524f).ToVector3f(),
                    Orientation = new Quaternion(-0.05315936f, -0.1231034f, 0.04981349f, 0.9897162f).ToQuatf()
                }
            };

            _skeleton.Bones[13] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Ring2, ParentBoneIndex = 12,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.0389961f, -4.66E-10f, -5.24E-10f).ToVector3f(),
                    Orientation = new Quaternion(-0.03363252f, -0.00278984f, 0.00567602f, 0.9994143f).ToQuatf()
                }
            };

            _skeleton.Bones[14] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Ring3, ParentBoneIndex = 13,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.02657339f, -1.281E-09f, -1.63E-09f).ToVector3f(),
                    Orientation = new Quaternion(-0.003477462f, 0.02917945f, -0.02502854f, 0.9992548f).ToQuatf()
                }
            };
            _skeleton.Bones[15] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Pinky0, ParentBoneIndex = 0,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.03407356f, -0.009419835f, -0.02299858f).ToVector3f(),
                    Orientation = new Quaternion(-0.207036f, -0.1403428f, 0.0183118f, 0.9680417f).ToQuatf()
                }
            };

            _skeleton.Bones[16] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Pinky1, ParentBoneIndex = 15,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.04565055f, -9.98611E-07f, 2.193963E-06f).ToVector3f(),
                    Orientation = new Quaternion(0.09111304f, 0.00407137f, 0.02812923f, 0.9954349f).ToQuatf()
                }
            };

            _skeleton.Bones[17] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Pinky2, ParentBoneIndex = 16,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.03072042f, 6.98E-10f, 1.106E-09f).ToVector3f(),
                    Orientation = new Quaternion(-0.03761665f, -0.04293772f, -0.01328605f, 0.9982809f).ToQuatf()
                }
            };

            _skeleton.Bones[18] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_Pinky3, ParentBoneIndex = 17,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.02031138f, -1.455E-09f, -1.397E-09f).ToVector3f(),
                    Orientation = new Quaternion(0.0006447434f, 0.04917067f, -0.02401883f, 0.9985014f).ToQuatf()
                }
            };

            _skeleton.Bones[19] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_MaxSkinnable, ParentBoneIndex = 5,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.02459077f, 0.001026974f, -0.0006703701f).ToVector3f(),
                    Orientation = new Quaternion(0f, 0f, 0f, 1f).ToQuatf()
                }
            };

            _skeleton.Bones[20] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_IndexTip, ParentBoneIndex = 8,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.02236338f, 0.00102507f, -0.0002956076f).ToVector3f(),
                    Orientation = new Quaternion(0f, 0f, 0f, 1f).ToQuatf()
                }
            };

            _skeleton.Bones[21] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_MiddleTip, ParentBoneIndex = 11,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.02496492f, 0.001137299f, -0.0003086528f).ToVector3f(),
                    Orientation = new Quaternion(0f, 0f, 0f, 1f).ToQuatf()
                }
            };

            _skeleton.Bones[22] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_RingTip, ParentBoneIndex = 14,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.02432613f, 0.001608172f, -0.000257905f).ToVector3f(),
                    Orientation = new Quaternion(0f, 0f, 0f, 1f).ToQuatf()
                }
            };

            _skeleton.Bones[23] = new OVRPlugin.Bone() {
                Id = OVRSkeleton.BoneId.Hand_PinkyTip, ParentBoneIndex = 18,
                Pose = new OVRPlugin.Posef() {
                    Position = new Vector3(-0.02192238f, 0.001216086f, 0.0002464796f).ToVector3f(),
                    Orientation = new Quaternion(0f, 0f, 0f, 1f).ToQuatf()
                }
            };
        }


        return true;
    }


    private void InitializeBindPose() {
        if (!_bindPosesGO) {
            _bindPosesGO = new GameObject("BindPoses");
            _bindPosesGO.transform.SetParent(transform, false);
            _bindPosesGO.transform.localPosition = Vector3.zero;
            _bindPosesGO.transform.localRotation = Quaternion.identity;
        }

        if (_bindPoses == null || _bindPoses.Count != _bones.Count) {
            _bindPoses = new List<OVRBone>(new OVRBone[_bones.Count]);
            BindPoses = _bindPoses.AsReadOnly();
        }

        // pre-populate bones list before attempting to apply bone hierarchy
        for (int i = 0; i < _bindPoses.Count; ++i) {
            OVRBone bone = _bones[i];
            OVRBone bindPoseBone = _bindPoses[i] ?? (_bindPoses[i] = new OVRBone());
            bindPoseBone.Id = bone.Id;
            bindPoseBone.ParentBoneIndex = bone.ParentBoneIndex;

            Transform trans = bindPoseBone.Transform ?? (bindPoseBone.Transform =
                new GameObject(OVRSkeleton.BoneLabelFromBoneId((OVRSkeleton.SkeletonType) _skeletonType,
                    bindPoseBone.Id)).transform);
            trans.localPosition = bone.Transform.localPosition;
            trans.localRotation = bone.Transform.localRotation;
        }

        for (int i = 0; i < _bindPoses.Count; ++i) {
            if ((OVRSkeleton.BoneId) _bindPoses[i].ParentBoneIndex == OVRSkeleton.BoneId.Invalid) {
                _bindPoses[i].Transform.SetParent(_bindPosesGO.transform, false);
            }
            else { _bindPoses[i].Transform.SetParent(_bindPoses[_bindPoses[i].ParentBoneIndex].Transform, false); }
        }
    }

    private void Update() {
#if UNITY_EDITOR
        if (ShouldInitialize()) { Initialize(); }
#endif

        if (!IsInitialized || _dataProvider == null) {
            IsDataValid = false;
            IsDataHighConfidence = false;

            return;
        }

        var data = _dataProvider.GetSkeletonPoseData();

        IsDataValid = data.IsDataValid;
        if (data.IsDataValid) {
            if (SkeletonChangedCount != data.SkeletonChangedCount) {
                SkeletonChangedCount = data.SkeletonChangedCount;
                IsInitialized = false;
                Initialize();
            }

            IsDataHighConfidence = data.IsDataHighConfidence;

            if (_updateRootPose) {
                transform.localPosition = data.RootPose.Position.FromFlippedZVector3f();
                transform.localRotation = data.RootPose.Orientation.FromFlippedZQuatf();
            }

            if (_updateRootScale) {
                transform.localScale = new Vector3(data.RootScale, data.RootScale, data.RootScale);
            }

            for (var i = 0; i < _bones.Count; ++i) {
                if (_bones[i].Transform != null) {
                    if (_skeletonType == OVRPlugin.SkeletonType.HandLeft ||
                        _skeletonType == OVRPlugin.SkeletonType.HandRight) {
                        _bones[i].Transform.localRotation = data.BoneRotations[i].FromFlippedXQuatf();

                        if (_bones[i].Id == OVRSkeleton.BoneId.Hand_WristRoot) {
                            _bones[i].Transform.localRotation *= wristFixupRotation;
                        }
                    }
                    else { _bones[i].Transform.localRotation = data.BoneRotations[i].FromFlippedZQuatf(); }
                }
            }
        }
    }


    public OVRSkeleton.BoneId GetCurrentStartBoneId() {
        switch (_skeletonType) {
            case OVRPlugin.SkeletonType.HandLeft:
            case OVRPlugin.SkeletonType.HandRight:
                return OVRSkeleton.BoneId.Hand_Start;
            case OVRPlugin.SkeletonType.None:
            default:
                return OVRSkeleton.BoneId.Invalid;
        }
    }

    public OVRSkeleton.BoneId GetCurrentEndBoneId() {
        switch (_skeletonType) {
            case OVRPlugin.SkeletonType.HandLeft:
            case OVRPlugin.SkeletonType.HandRight:
                return OVRSkeleton.BoneId.Hand_End;
            case OVRPlugin.SkeletonType.None:
            default:
                return OVRSkeleton.BoneId.Invalid;
        }
    }
}