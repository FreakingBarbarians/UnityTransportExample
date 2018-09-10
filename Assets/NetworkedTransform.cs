using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class NetworkedTransform : StateSynchronizedMonoBehavior {
    private MoveState Move;

    public override void Synchronize(byte[] state)
    {

        NetworkState net_state = NetEngine.ByteArrayToStructure<NetworkState>(state);
        // interp later but set for now!
        this.transform.position = net_state.Pos;
        this.transform.rotation = net_state.Rotation;
    }

    public override byte[] GetState(out int size)
    {
        NetworkState state;
        PacketHeader packet;
        packet.type = MessageType.ENTITY_STATE;
        packet.nodeSource = NetEngine.instance.node_id;
        state.Header = packet;
        state.Pos = transform.position;
        state.Rotation = transform.rotation;
        state.SimTick = 0;
        state.NET_ID = NET_ID;
        size = Marshal.SizeOf(state);
        return NetEngine.GetBytes<NetworkState>(state);
    }
}