// modified from MetaMovement OVRBody.cs

using System;
using System.IO;
using Unity.Netcode;
using UnityEngine;

namespace ChiVR.Network
{
    #region Helper methods

    internal class SkeletonPoseDataHelper
    {
        public static void WritePosef(BinaryWriter writer, OVRPlugin.Posef pose)
        {
            writer.Write(pose.Position.x);
            writer.Write(pose.Position.y);
            writer.Write(pose.Position.z);
            writer.Write(pose.Orientation.x);
            writer.Write(pose.Orientation.y);
            writer.Write(pose.Orientation.z);
            writer.Write(pose.Orientation.w);
        }

        public static OVRPlugin.Posef ReadPosef(BinaryReader reader)
        {
            OVRPlugin.Posef pose;
            pose.Position.x = reader.ReadSingle();
            pose.Position.y = reader.ReadSingle();
            pose.Position.z = reader.ReadSingle();
            pose.Orientation.x = reader.ReadSingle();
            pose.Orientation.y = reader.ReadSingle();
            pose.Orientation.z = reader.ReadSingle();
            pose.Orientation.w = reader.ReadSingle();
            return pose;
        }

        public static void WriteQuatf(BinaryWriter writer, OVRPlugin.Quatf quat)
        {
            writer.Write(quat.x);
            writer.Write(quat.y);
            writer.Write(quat.z);
            writer.Write(quat.w);
        }

        public static OVRPlugin.Quatf ReadQuatf(BinaryReader reader)
        {
            OVRPlugin.Quatf quat;
            quat.x = reader.ReadSingle();
            quat.y = reader.ReadSingle();
            quat.z = reader.ReadSingle();
            quat.w = reader.ReadSingle();
            return quat;
        }

        public static void WriteVector3f(BinaryWriter writer, OVRPlugin.Vector3f vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        public static OVRPlugin.Vector3f ReadVector3f(BinaryReader reader)
        {
            OVRPlugin.Vector3f vector;
            vector.x = reader.ReadSingle();
            vector.y = reader.ReadSingle();
            vector.z = reader.ReadSingle();
            return vector;
        }

        public static byte[] SerializeSkeletonPoseData(OVRSkeleton.SkeletonPoseData data)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    // Write RootPose
                    WritePosef(writer, data.RootPose);

                    // Write RootScale
                    writer.Write(data.RootScale);

                    // Write BoneRotations array
                    writer.Write(data.BoneRotations.Length);
                    foreach (var rotation in data.BoneRotations)
                    {
                        WriteQuatf(writer, rotation);
                    }

                    // Write IsDataValid and IsDataHighConfidence
                    writer.Write(data.IsDataValid);
                    writer.Write(data.IsDataHighConfidence);

                    // Write BoneTranslations array
                    writer.Write(data.BoneTranslations.Length);
                    foreach (var translation in data.BoneTranslations)
                    {
                        WriteVector3f(writer, translation);
                    }

                    // Write SkeletonChangedCount
                    writer.Write(data.SkeletonChangedCount);
                }

                return stream.ToArray(); // Return the serialized byte array
            }
        }

        public static OVRSkeleton.SkeletonPoseData DeserializeSkeletonPoseData(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    OVRSkeleton.SkeletonPoseData poseData = new OVRSkeleton.SkeletonPoseData();

                    // Read RootPose
                    poseData.RootPose = ReadPosef(reader);

                    // Read RootScale
                    poseData.RootScale = reader.ReadSingle();

                    // Read BoneRotations array
                    int boneRotationsLength = reader.ReadInt32();
                    poseData.BoneRotations = new OVRPlugin.Quatf[boneRotationsLength];
                    for (int i = 0; i < boneRotationsLength; i++)
                    {
                        poseData.BoneRotations[i] = ReadQuatf(reader);
                    }

                    // Read IsDataValid and IsDataHighConfidence
                    poseData.IsDataValid = reader.ReadBoolean();
                    poseData.IsDataHighConfidence = reader.ReadBoolean();

                    // Read BoneTranslations array
                    int boneTranslationsLength = reader.ReadInt32();
                    poseData.BoneTranslations = new OVRPlugin.Vector3f[boneTranslationsLength];
                    for (int i = 0; i < boneTranslationsLength; i++)
                    {
                        poseData.BoneTranslations[i] = ReadVector3f(reader);
                    }

                    // Read SkeletonChangedCount
                    poseData.SkeletonChangedCount = reader.ReadInt32();

                    return poseData; // Return the deserialized SkeletonPoseData
                }
            }
        }
    }

    #endregion

    public class NetworkOVRBody : NetworkBehaviour,
        OVRSkeleton.IOVRSkeletonDataProvider,
        OVRSkeletonRenderer.IOVRSkeletonRendererDataProvider
    {
        private OVRPlugin.BodyState _bodyState;

        private OVRPlugin.Quatf[] _boneRotations;

        private OVRPlugin.Vector3f[] _boneTranslations;

        private bool _dataChangedSinceLastQuery;

        private bool _hasData;

        private const OVRPermissionsRequester.Permission BodyTrackingPermission =
            OVRPermissionsRequester.Permission.BodyTracking;

        private Action<string> _onPermissionGranted;

        [SerializeField]
        [Tooltip(
            "The skeleton data type to be provided. Should be sync with OVRSkeleton. For selecting the tracking mode on the device, check settings in OVRManager.")]
        private OVRPlugin.BodyJointSet _providedSkeletonType = OVRPlugin.BodyJointSet.UpperBody;

        public OVRPlugin.BodyJointSet ProvidedSkeletonType
        {
            get => _providedSkeletonType;
            set => _providedSkeletonType = value;
        }

        private static int _trackingInstanceCount;

        #region Modification for multiplayer

        private byte[] _streamedData;

        public void SetStreamData(byte[] bytes)
        {
            _streamedData = bytes;
        }

        private AvatarBehaviourNGO _avatarBehaviour;

        private float _cycleStartTime;

        private float _intervalToSendDataInSec = 0.08f;

        #endregion

        /// <summary>
        /// The raw <see cref="BodyState"/> data used to populate the <see cref="OVRSkeleton"/>.
        /// </summary>
        public OVRPlugin.BodyState? BodyState => _hasData ? _bodyState : default(OVRPlugin.BodyState?);

        private void Awake()
        {
            _avatarBehaviour = gameObject.GetComponent<AvatarBehaviourNGO>();
            _onPermissionGranted = OnPermissionGranted;
        }

        private void OnEnable()
        {
            _dataChangedSinceLastQuery = false;
            _hasData = false;
            var manager = FindObjectOfType<OVRManager>();
            if (manager != null && manager.SimultaneousHandsAndControllersEnabled)
            {
                Debug.LogWarning(
                    "Currently, Body API and simultaneous hands and controllers cannot be enabled at the same time",
                    this);
                enabled = false;
                return;
            }

            if (_providedSkeletonType == OVRPlugin.BodyJointSet.FullBody &&
                OVRRuntimeSettings.GetRuntimeSettings().BodyTrackingJointSet == OVRPlugin.BodyJointSet.UpperBody)
            {
                Debug.LogWarning(
                    $"[{nameof(OVRBody)}] Full body skeleton is used, but Full body tracking is disabled. Check settings in OVRManager.");
            }

            _trackingInstanceCount++;
            if (!StartBodyTracking())
            {
                enabled = false;
                return;
            }

            if (OVRPlugin.nativeXrApi == OVRPlugin.XrApi.OpenXR)
            {
                GetBodyState(OVRPlugin.Step.Render);
            }
            else
            {
                enabled = false;
                Debug.LogWarning($"[{nameof(OVRBody)}] Body tracking is only supported by OpenXR and is unavailable.");
            }
        }

        private void OnPermissionGranted(string permissionId)
        {
            if (permissionId == OVRPermissionsRequester.GetPermissionId(BodyTrackingPermission))
            {
                OVRPermissionsRequester.PermissionGranted -= _onPermissionGranted;
                enabled = true;
            }
        }

        private static bool StartBodyTracking()
        {
            OVRPlugin.BodyJointSet jointSet = OVRRuntimeSettings.GetRuntimeSettings().BodyTrackingJointSet;
            if (!OVRPlugin.StartBodyTracking2(jointSet))
            {
                Debug.LogWarning(
                    $"[{nameof(OVRBody)}] Failed to start body tracking with joint set {jointSet}.");
                return false;
            }

            OVRPlugin.BodyTrackingFidelity2 fidelity = OVRRuntimeSettings.GetRuntimeSettings().BodyTrackingFidelity;
            bool fidelityChangeSuccessful = OVRPlugin.RequestBodyTrackingFidelity(fidelity);
            if (!fidelityChangeSuccessful)
            {
                // Fidelity suggestion failed but body tracking might still work.
                Debug.LogWarning($"[{nameof(OVRBody)}] Failed to set Body Tracking fidelity to: {fidelity}");
            }

            return true;
        }

        private void OnDisable()
        {

            if (--_trackingInstanceCount == 0)
            {
                OVRPlugin.StopBodyTracking();
            }
        }

        private void OnDestroy()
        {
            OVRPermissionsRequester.PermissionGranted -= _onPermissionGranted;
        }

        private void Update() => GetBodyState(OVRPlugin.Step.Render);

        public static bool SetRequestedJointSet(OVRPlugin.BodyJointSet jointSet)
        {
            var activeJointSet = OVRRuntimeSettings.GetRuntimeSettings().BodyTrackingJointSet;
            if (jointSet != activeJointSet)
            {
                OVRRuntimeSettings.GetRuntimeSettings().BodyTrackingJointSet = jointSet;
                if (_trackingInstanceCount > 0)
                {
                    OVRPlugin.StopBodyTracking();
                    return StartBodyTracking();
                }
            }

            return true;
        }


        public static bool SuggestBodyTrackingCalibrationOverride(float height) =>
            OVRPlugin.SuggestBodyTrackingCalibrationOverride(new OVRPlugin.BodyTrackingCalibrationInfo
                { BodyHeight = height });

        public static bool ResetBodyTrackingCalibration() => OVRPlugin.ResetBodyTrackingCalibration();

        public OVRPlugin.BodyTrackingCalibrationState GetBodyTrackingCalibrationStatus()
        {
            if (!_hasData)
                return OVRPlugin.BodyTrackingCalibrationState.Invalid;

            return _bodyState.CalibrationStatus;
        }

        public OVRPlugin.BodyTrackingFidelity2 GetBodyTrackingFidelityStatus()
        {
            return _bodyState.Fidelity;
        }

        private void GetBodyState(OVRPlugin.Step step)
        {
            if (OVRPlugin.GetBodyState4(step, _providedSkeletonType, ref _bodyState))
            {
                _hasData = true;
                _dataChangedSinceLastQuery = true;
            }
            else
            {
                _hasData = false;
            }

        }

        OVRSkeleton.SkeletonType OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonType()
        {
            return _providedSkeletonType switch
            {
                OVRPlugin.BodyJointSet.UpperBody => OVRSkeleton.SkeletonType.Body,
                OVRPlugin.BodyJointSet.FullBody => OVRSkeleton.SkeletonType.FullBody,
                _ => OVRSkeleton.SkeletonType.None,
            };
        }

        #region Modification for multiplayer

        OVRSkeleton.SkeletonPoseData OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonPoseData()
        {
            if (!NetworkManager.isActiveAndEnabled || IsOwner)
            {

                if (!_hasData)
                    return default;

                if (_dataChangedSinceLastQuery)
                {
                    // Make sure arrays have been allocated
                    Array.Resize(ref _boneRotations, _bodyState.JointLocations.Length);
                    Array.Resize(ref _boneTranslations, _bodyState.JointLocations.Length);

                    // Copy joint poses into bone arrays
                    for (var i = 0; i < _bodyState.JointLocations.Length; i++)
                    {
                        var jointLocation = _bodyState.JointLocations[i];
                        if (jointLocation.OrientationValid)
                        {
                            _boneRotations[i] = jointLocation.Pose.Orientation;
                        }

                        if (jointLocation.PositionValid)
                        {
                            _boneTranslations[i] = jointLocation.Pose.Position;
                        }
                    }

                    _dataChangedSinceLastQuery = false;
                }

                // render locally
                var data = new OVRSkeleton.SkeletonPoseData
                {
                    IsDataValid = true,
                    IsDataHighConfidence = _bodyState.Confidence > .5f,
                    RootPose = _bodyState.JointLocations[(int)OVRPlugin.BoneId.Body_Root].Pose,
                    RootScale = 1.0f,
                    BoneRotations = _boneRotations,
                    BoneTranslations = _boneTranslations,
                    SkeletonChangedCount = (int)_bodyState.SkeletonChangedCount,
                };

                var elapsedTime = Time.time - _cycleStartTime;

                if (elapsedTime >= _intervalToSendDataInSec)
                {
                    // serialize it
                    var serialzied = SkeletonPoseDataHelper.SerializeSkeletonPoseData(data);
                    _avatarBehaviour.ReceiveStreamData(serialzied);
                }

                return data;
            }

            if (_streamedData != null)
            {
                // use synchronized
                var deserialized = SkeletonPoseDataHelper.DeserializeSkeletonPoseData(_streamedData);
                _streamedData = null;
                return deserialized;
            }

            return default;
        }

        #endregion

        OVRSkeletonRenderer.SkeletonRendererData
            OVRSkeletonRenderer.IOVRSkeletonRendererDataProvider.GetSkeletonRendererData() => _hasData
            ? new OVRSkeletonRenderer.SkeletonRendererData
            {
                RootScale = 1.0f,
                IsDataValid = true,
                IsDataHighConfidence = true,
                ShouldUseSystemGestureMaterial = false,
            }
            : default;

        /// <summary>
        /// Body Tracking Fidelity defines the quality of the tracking
        /// </summary>
        public static OVRPlugin.BodyTrackingFidelity2 Fidelity
        {
            get => OVRRuntimeSettings.GetRuntimeSettings().BodyTrackingFidelity;
            set
            {
                OVRRuntimeSettings.GetRuntimeSettings().BodyTrackingFidelity = value;
                OVRPlugin.RequestBodyTrackingFidelity(value);
            }
        }
    }
}