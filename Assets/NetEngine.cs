using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

public class NetEngine : MonoBehaviour {
    public delegate void NetworkHandler(int hostId, int connectionId, int channelId, byte[] buffer, int bufferSize, int recieved, byte error);
    public static NetEngine instance;
    public Dictionary<MessageType, NetworkHandler> Handlers;
    public List<StateSynchronizedMonoBehavior> LocalEntities = new List<StateSynchronizedMonoBehavior>();
    public Dictionary<int, StateSynchronizedMonoBehavior> Synchronizable = new Dictionary<int, StateSynchronizedMonoBehavior>();
    public ulong SimFrame;
    public Dictionary<int, GameObject> SpawnablePrefabs = new Dictionary<int, GameObject>();

    private static int BUFFER_SIZE = 1024;

    public int node_id = -1;
    private int next_id = 1;
    public int next_net_id = 0;

    bool is_server = false;
    bool started = false;
    ConnectionConfig cconfig;
    int TCP_id = -1;
    int UDP_id = -1;
    int SOCK_id = -1;
    int PORT = -1;
    HostTopology topo;
    int CONN_id = -1;
    int SELF_CONN_id = -1;
    private bool socket_open = false;
    private byte[] buffer = new byte[BUFFER_SIZE];

    private void Start()
    {
        instance = this;
        Init();
    }

    private void Init() {
        Application.runInBackground = true;
        Synchronizable = new Dictionary<int, StateSynchronizedMonoBehavior>();
        LocalEntities = new List<StateSynchronizedMonoBehavior>();
        Handlers = new Dictionary<MessageType, NetworkHandler>();
        Handlers.Add(MessageType.ENTITY_STATE, new NetworkHandler(SynchronizeEntity));
        Handlers.Add(MessageType.SET_NODE_ID, new NetworkHandler(RecvSetNetID));
        Handlers.Add(MessageType.PING, new NetworkHandler(RecvPing));
        Handlers.Add(MessageType.SPAWN, new NetworkHandler(RecvSpawn));
    }

    public void Destroy() {
        NetworkTransport.Shutdown();
    }

    private void OnDestroy()
    {
        Destroy();
    }

    void FixedUpdate() {
        if (!socket_open) {
            return;
        }
        int hostId, connectionId, channelId, recieved; byte error;
        NetworkEventType net;
        while ((net = NetworkTransport.Receive(out hostId, out connectionId, out channelId, buffer, BUFFER_SIZE, out recieved, out error)) != NetworkEventType.Nothing) {
            if ((NetworkError)error != NetworkError.Ok) {
                continue;
            }

            switch (net) {
                case NetworkEventType.DataEvent:
                    PacketHeader head = ByteArrayToStructure<PacketHeader>(buffer);
                    if (recieved >= 1)
                    {
                        Handlers[head.type].Invoke(hostId, connectionId, channelId, buffer, BUFFER_SIZE, recieved, error);
                    }
                    else
                    {
                        // some error happened
                    }
                    break;
                case NetworkEventType.ConnectEvent:
                    Debug.Log("Recieved Connection : " + hostId + " " + connectionId + " " + channelId);
                    if (connectionId == SELF_CONN_id)
                    {
                        break;
                    }

                    CONN_id = connectionId;

                    if (is_server) {
                        PacketHeader reply_head;
                        reply_head.nodeSource = node_id;
                        reply_head.type = MessageType.SET_NODE_ID;
                        SetNodeID reply_sni;
                        reply_sni.Header = reply_head;
                        reply_sni.NET_ID = next_id;
                        if (SendData(GetBytes<SetNodeID>(reply_sni), Marshal.SizeOf(reply_sni), connectionId)) {
                            next_id++;
                        }
                        foreach (StateSynchronizedMonoBehavior mono in LocalEntities) {
                            PacketHeader pack;
                            pack.nodeSource = node_id;
                            pack.type = MessageType.SPAWN;
                            SpawnPrefab spawn;
                            spawn.header = pack;
                            spawn.local_auth_node_id = node_id;
                            spawn.net_id = mono.NET_ID;
                            spawn.pos = mono.transform.position;
                            spawn.prefab_id = mono.prefab_id;
                            spawn.rot = mono.transform.rotation;
                            byte[] b = GetBytes(spawn);
                            SendData(b, Marshal.SizeOf(spawn), CONN_id);
                        }
                        foreach (StateSynchronizedMonoBehavior mono in Synchronizable.Values) {
                            PacketHeader pack;
                            pack.nodeSource = node_id;
                            pack.type = MessageType.SPAWN;
                            SpawnPrefab spawn;
                            spawn.header = pack;
                            spawn.local_auth_node_id = node_id;
                            spawn.net_id = mono.NET_ID;
                            spawn.pos = mono.transform.position;
                            spawn.prefab_id = mono.prefab_id;
                            spawn.rot = mono.transform.rotation;
                            byte[] b = GetBytes(spawn);
                            SendData(b, Marshal.SizeOf(spawn), CONN_id);
                        }
                    }
                    break;
            }            
        }

        if (CONN_id >= 0)
        {
            foreach (StateSynchronizedMonoBehavior local in LocalEntities)
            {
                int size;
                byte[] netstate = local.GetState(out size);
                SendData(netstate, size, CONN_id);
            }
        }
    }

    private void Update()
    {
    }

    private bool SendData(byte[] data, int size, int connection) {

        if (!socket_open) {
            return false;
        }

        if (size > BUFFER_SIZE) {
            Debug.LogWarning("DATA TOO BIG!: " + size);
            return false;
        }
        Buffer.BlockCopy(data, 0, buffer, 0, size);
        byte error;
        NetworkTransport.Send(SOCK_id, connection, UDP_id, buffer, size, out error);
        if ((NetworkError)error != NetworkError.Ok) {
            Debug.LogWarning("Sending errored out");
            return false;
        }
        return true;
    }

    public void Synchronize(ulong frame_num) {
        SimFrame = frame_num;
    }

    public void Connect(string ip, int port) {
        byte error;
        CONN_id = NetworkTransport.Connect(SOCK_id, ip, port, 0, out error);
        if ((NetworkError)error != NetworkError.Ok)
        {
            Debug.Log("Fatal Error");
        }
        else
        {
            Debug.Log("All good!");
        }
    }

    private void SelfConnect() {
        Debug.Log("Selfing");
        byte error;
        SELF_CONN_id = NetworkTransport.Connect(SOCK_id, "127.0.0.1", PORT, 0, out error);
        if ((NetworkError)error != NetworkError.Ok)
        {
            Debug.Log("Fatal Error Self Connect");
        }
        else
        {
            Debug.Log("All good!");
        }
    }

    public void StartSocket(int port) {
        if (socket_open) {
            return;
        }
        NetworkTransport.Init();
        cconfig = new ConnectionConfig();
        TCP_id = cconfig.AddChannel(QosType.Reliable);
        UDP_id = cconfig.AddChannel(QosType.Unreliable);
        topo = new HostTopology(cconfig, 10);
        SOCK_id = NetworkTransport.AddHost(topo, port);
        socket_open = true;
        this.PORT = port;
        SelfConnect();
    }

    public void StartServer(int port) {
        if (socket_open)
        {
            return;
        }
        StartSocket(port);
        is_server = true;
        node_id = 0;
    }

    public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
    {
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        }
        finally
        {
            handle.Free();
        }
    }

    public static byte[] GetBytes<T>(T str) where T : struct
    {
        int size = Marshal.SizeOf(str);

        byte[] arr = new byte[size];

        GCHandle h = default(GCHandle);

        try
        {
            h = GCHandle.Alloc(arr, GCHandleType.Pinned);

            Marshal.StructureToPtr(str, h.AddrOfPinnedObject(), false);
        }
        finally
        {
            if (h.IsAllocated)
            {
                h.Free();
            }
        }

        return arr;
    }

    public void SynchronizeEntity(int hostId, int connectionId, int channelId, byte[] buffer, int bufferSize, int recieved, byte error) {
        NetworkState netstate = ByteArrayToStructure<NetworkState>(buffer);
        if (Synchronizable.ContainsKey(netstate.NET_ID)) {
            Synchronizable[netstate.NET_ID].Synchronize(buffer);
        } else {
            Debug.LogWarning("Unrecognizable NetID: " + netstate.NET_ID);
        }
    }
    public void RecvPing(int hostId, int connectionId, int channelId, byte[] buffer, int bufferSize, int recieved, byte error) {
        Ping ping = ByteArrayToStructure<Ping>(buffer);
        if (ping.header.nodeSource == node_id) {
            ping.turnaround_time = Time.time;
            Debug.Log("RTT: " + (ping.turnaround_time - ping.send_time) * 1000 + "ms");
        } else
        {
            if (!SendData(GetBytes<Ping>(ping), recieved, connectionId)) {
                Debug.LogWarning("Failed to reply to ping");
            }
        }
    }
    public void RecvSetNetID(int hostId, int connectionId, int channelId, byte[] buffer, int bufferSize, int recieved, byte error) {
        if (is_server) {
            Debug.LogWarning("Some idiot's trying to set server id");
            return;
        }
        SetNodeID sni = ByteArrayToStructure<SetNodeID>(buffer);
        this.node_id = sni.NET_ID;
        Debug.Log("Recieved Authoratative Net_Id: " + sni.NET_ID);
    }
    public void RecvSpawn(int hostId, int connectionId, int channelId, byte[] buffer, int bufferSize, int recieved, byte error) {
        SpawnPrefab fab = ByteArrayToStructure<SpawnPrefab>(buffer);
        Debug.Log(fab.prefab_id);
        GameObject go = GameObject.Instantiate(SpawnablePrefabs[fab.prefab_id]);
        NetworkedTransform mono = go.GetComponent<NetworkedTransform>();
        mono.prefab_id = fab.prefab_id;
        mono.NET_ID = fab.net_id;
        if (fab.local_auth_node_id == node_id)
        {
            mono.LocalAuthority = true;
            LocalEntities.Add(mono);
        }
        else {
            mono.LocalAuthority = false;
            Synchronizable[mono.NET_ID] = mono;
        }
    }
    public void PingServer() {
        PacketHeader head;
        head.nodeSource = node_id;
        head.type = MessageType.PING;
        Ping ping;
        ping.header = head;
        ping.send_time = Time.time;
        ping.turnaround_time = -1;
        SendData(GetBytes<Ping>(ping), Marshal.SizeOf(ping), CONN_id);
    }
    public void SpawnGO(int go_id, Vector3 pos, Quaternion rot, int local_auth)
    {
        PacketHeader ph;
        ph.nodeSource = node_id;
        ph.type = MessageType.SPAWN;
        SpawnPrefab sp;
        sp.header = ph;
        sp.local_auth_node_id = local_auth;
        sp.net_id = next_id;
        sp.pos = pos;
        sp.rot = rot;
        sp.prefab_id = go_id;
        byte[] dat = GetBytes<SpawnPrefab>(sp);

        if (SendData(dat, Marshal.SizeOf(sp), SELF_CONN_id) &&
        SendData(dat, Marshal.SizeOf(sp), CONN_id)) {
            next_net_id++;
        }

    }
}