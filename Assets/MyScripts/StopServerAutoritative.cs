using Unity.Netcode.Components;

public class StopServerAutoritative : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
