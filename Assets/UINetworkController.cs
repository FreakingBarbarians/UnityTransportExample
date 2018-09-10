using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UINetworkController : MonoBehaviour {
    public InputField Port;
    public InputField IP;

    public void StartServer() {
        NetEngine.instance.StartServer(int.Parse(Port.text));
    }

    public void StartClient() {
        NetEngine.instance.StartSocket(int.Parse(Port.text));
    }

    public void Connect() {
        NetEngine.instance.Connect(IP.text, int.Parse(Port.text));
    }

    public void SendPing() {
        NetEngine.instance.PingServer();
    }

    public void SpawnSphere() {
        NetEngine.instance.SpawnGO(0, Vector3.zero, Quaternion.identity, NetEngine.instance.node_id);
    }
}
