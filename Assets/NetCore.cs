using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

public abstract class StateSynchronizedMonoBehavior : MonoBehaviour
{
    public int prefab_id;
    public int NET_ID;
    public bool LocalAuthority = false;
    public abstract void Synchronize(byte[] state);
    public abstract byte[] GetState(out int size);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PacketHeader
{
    public MessageType type;
    public int nodeSource;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NetworkState
{
    public PacketHeader Header;
    public int NET_ID;
    public Vector3 Pos;
    public Quaternion Rotation;
    public ulong SimTick;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)] 
public struct Ping {
    public PacketHeader header;
    public float send_time;
    public float turnaround_time;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SetNodeID {
    public PacketHeader Header;
    public int NET_ID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SpawnPrefab {
    public PacketHeader header;
    public int prefab_id;
    public int net_id;
    public int local_auth_node_id;
    public Vector3 pos;
    public Quaternion rot;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SimpleMoveState {
    public NetworkState state;
    public MoveState move;
}


public enum MoveState : byte {
    UP,
    DOWN,
    LEFT,
    RIGHT,
    UP_LEFT,
    UP_RIGHT,
    DOWN_LEFT,
    DOWN_RIGHT,
    NEUTRAL
}

public enum MessageType : byte {
    ENTITY_STATE,
    PING,
    SET_NODE_ID,
    SPAWN
}