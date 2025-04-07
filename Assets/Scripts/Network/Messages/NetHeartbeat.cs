using System;
using System.Collections.Generic;
using UnityEngine;

namespace Network.Messages
{
    public class NetHeartbeat : IMessage<float>
    {
        private float _timestamp;
    
        public NetHeartbeat() { }
    
        public MessageType GetMessageType()
        {
            return MessageType.Heartbeat;
        }
    
        public byte[] Serialize()
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes((int)GetMessageType()));
            data.AddRange(BitConverter.GetBytes(Time.realtimeSinceStartup));
            return data.ToArray();
        }
    
        public float Deserialize(byte[] message)
        {
            int offset = 4;
            _timestamp = BitConverter.ToSingle(message, offset);
            return _timestamp;
        }
    }
}