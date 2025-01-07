// refer to: Meta.XR.MultiplayerBlocks.NGO.AvatarBehaviourNGO

using Unity.Netcode;
using UnityEngine;

namespace ChiVR.Network
{
    public class AvatarBehaviourNGO : NetworkBehaviour
    {
        private NetworkAvatarDataStream _avatarDataStream;

        private NetworkOVRBody _networkOvrBody;

        private Transform _cameraRig;

        private void Awake()
        {
            _avatarDataStream = new NetworkAvatarDataStream(readPerm: NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

            if (OVRManager.instance)
            {
                _cameraRig = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().transform;
            }

            _networkOvrBody = GetComponent<NetworkOVRBody>();
        }

        public override void OnNetworkSpawn()
        {
            _avatarDataStream.OnDataChanged += OnAvatarDataStreamChanged;
        }

        public override void OnNetworkDespawn()
        {
            _avatarDataStream.OnDataChanged -= OnAvatarDataStreamChanged;
        }

        private void OnAvatarDataStreamChanged()
        {
            if (IsOwner)
            {
                return;
            }

            _networkOvrBody.SetStreamData(_avatarDataStream.Value);
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            if (_cameraRig == null)
            {
                return;
            }

            var t = transform;
            t.position = _cameraRig.position;
            t.rotation = _cameraRig.rotation;
        }

        public bool HasInputAuthority => IsOwner;

        public void ReceiveStreamData(byte[] bytes)
        {
            _avatarDataStream.Value = bytes;
        }
    }
}