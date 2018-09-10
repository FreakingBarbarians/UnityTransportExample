using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class Test : MonoBehaviour {

    static int BUFFER_SIZE = 1024;
    byte[] buffer = new byte[BUFFER_SIZE];
    GCHandle buff_handle;
    IntPtr buff_ptr;

    // Use this for initialization
    void Start () {
        buff_handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        buff_ptr = GCHandle.ToIntPtr(buff_handle);

        PacketHeader header;
        header.nodeSource = 42;
        header.type = MessageType.PING;
        SetNodeID sni;
        sni.Header = header;
        sni.NET_ID = 69;
        byte[] bytes = NetEngine.GetBytes<SetNodeID>(sni);
        PacketHeader deserialized = NetEngine.ByteArrayToStructure<PacketHeader>(bytes);
        Debug.Log(deserialized.nodeSource);
        Debug.Log(deserialized.type.ToString());
        SetNodeID sni_des = NetEngine.ByteArrayToStructure<SetNodeID>(bytes);
        Debug.Log(sni_des.NET_ID);
        Debug.Log(Marshal.SizeOf(sni_des));

        Buffer.BlockCopy(bytes, 0, buffer, 0, Marshal.SizeOf(sni_des));

        SetNodeID sni_des2 = NetEngine.ByteArrayToStructure<SetNodeID>(buffer);
        Debug.Log(sni_des2.NET_ID);
        Debug.Log(Marshal.SizeOf(sni_des2));
    }
}
