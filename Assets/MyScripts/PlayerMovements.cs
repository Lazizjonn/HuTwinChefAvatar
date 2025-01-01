using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR;
using Unity.Netcode.Transports.UTP;




public class PlayerMovements: NetworkBehaviour 
{
    public float moveSpeed = 6f;

    void Update()
    {
        if (!IsOwner) { return; }

        if (Input.GetKey(KeyCode.Z))
        {
            moveSpeed--;
        }

        if (Input.GetKey(KeyCode.X))
        {
            moveSpeed++;
        }

        if (Input.GetKey(KeyCode.I))
        {
            string IP = NetworkManager.GetComponent<UnityTransport>().ConnectionData.Address;
            Debug.Log("networkAddress: " + IP);
        }

        // Get input from arrow keys or WASD
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(horizontal, 0f, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(movement);

        // Get input from Oculus controllers
       /* Vector2 primaryAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector3 ContrMovement = new Vector3(primaryAxis.x, 0.0f, primaryAxis.y) * moveSpeed * Time.deltaTime;
        transform.Translate(ContrMovement);*/
    }


}