using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;
using AddressFamily = System.Net.Sockets.AddressFamily;
using TMPro;

public class NetworkManagerScript : MonoBehaviour
{
    string sharedLocalIpAddress = "127.0.0.1";
    public TMP_Text deviceIpAddress;
    public TMP_InputField ipTextField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 24;

        sharedLocalIpAddress = FindIpAddress();
        Debug.Log("host ip: " + sharedLocalIpAddress);
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = sharedLocalIpAddress;
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = (ushort)7777;

        deviceIpAddress.text = sharedLocalIpAddress;
        ipTextField.text = "";

        //NetworkManager.Singleton.StartHost();

        // This retrieves the address assigned to the client
        //string ipv4Address = NetworkManager.Singleton.LocalClientId.ToString();
        //Debug.Log("Start(): Host IP address: " + ipv4Address);
    }

    // Update is called once per frame
    void Update()
    {
       //
    }

    public void StartHost() 
    {
        Start();
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        string ipAddress = ipTextField.text;
        Debug.Log("client ip: " + ipAddress);
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = ipAddress;
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = (ushort)7777;
        NetworkManager.Singleton.StartClient();
    }

    public void Stop()
    {
        NetworkManager.Singleton.Shutdown();
    }

    /*private void FindIpAddress()
    {
        string myAddressLocal;
        string myAddressGlobal;

        //Get the local IP
        IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in hostEntry.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                myAddressLocal = ip.ToString();
                break;
            } //if
        } //foreach

        //Get the global IP
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.ipify.org");
        request.Method = "GET";
        request.Timeout = 1000; //time in ms
        try
        {
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                myAddressGlobal = reader.ReadToEnd();
            } //if
            else
            {
                Debug.LogError("FindIpAddress(): Timed out? " + response.StatusDescription);
                myAddressGlobal = "127.0.0.1";
            } //else
        } //try
        catch (WebException ex)
        {
            Debug.Log("FindIpAddress(): Likely no internet connection: " + ex.Message);
            myAddressGlobal = "127.0.0.1";
        } //catch

        //myAddressGlobal = new System.Net.WebClient().DownloadString("https://api.ipify.org"); 
        //single-line solution for the global IP, but long time-out when there is no internet connection,
        //so I prefer to do the method above where I can set a short time-out time
    }*/

    private string FindIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                Debug.Log("FindIpAddress2(): ip.ToString() = " + ip.ToString());
                return ip.ToString(); ;
            }
        }

        Debug.LogError("FindIpAddress2(): No network adapters with an IPv4 address in the system!");
        return sharedLocalIpAddress;
    }
}
