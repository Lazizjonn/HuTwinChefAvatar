using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SelfMadeFiles.Scripts.Network.components
{
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable)), RequireComponent(typeof(NetworkObject))]
    public class ClientInteractableOwnership : NetworkBehaviour
    {
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
            if (_interactable == null)
            {
                enabled = false;
            }
        }

        IEnumerator SetInteractionLayerTemporarily()
        {
            _interactable.interactionLayers = 0;
            Debug.Log("myVariable is now false");

            yield return new WaitForSeconds(1);
            _interactable.interactionLayers = LayerMask.GetMask("Default");
            Debug.Log("myVariable is now true");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _interactable.selectEntered.AddListener(ChangeOwnership);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _interactable.selectEntered.RemoveListener(ChangeOwnership);
        }

        private void ChangeOwnership(SelectEnterEventArgs args)
        {
            if (IsOwner)
            {
                return;
            }
            ChangeOwnershipServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ChangeOwnershipServerRpc(ulong clientId)
        {
            NetworkObject.ChangeOwnership(clientId);
            ChangeChildrenOwnership(NetworkObject.gameObject, clientId);
            ForceDropClientRpc();
        }

        private void ChangeChildrenOwnership(GameObject obj, ulong newOwnerId)
        {
            foreach (Transform child in obj.transform)
            {
                NetworkObject childNetworkObject = child.GetComponent<NetworkObject>();
                if (childNetworkObject != null)
                {
                    childNetworkObject.ChangeOwnership(newOwnerId);
                }

                ChangeChildrenOwnership(child.gameObject, newOwnerId);
            }
        }

        [ClientRpc]
        private void ForceDropClientRpc()
        {
            if (IsOwner)
            {
                return;
            }

            if (_interactable.isSelected)
            {
                SetInteractionLayerTemporarily();

                if (_interactable.interactorsSelecting.Count > 0)
                {
                    UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor = _interactable.interactorsSelecting[0]; // Get the first interactor

                    if (interactor != null && interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor xrBaseInteractor && xrBaseInteractor.isSelectActive)
                    {
                        _interactable.interactionManager.SelectExit(interactor, _interactable);
                    }
                }
            }
        }
    }
}