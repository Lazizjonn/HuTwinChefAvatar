// refer to: Meta.XR.MultiplayerBlocks.NGO.ClientNetworkTransform

using Unity.Netcode.Components;

namespace ChiVR.Network
{
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}