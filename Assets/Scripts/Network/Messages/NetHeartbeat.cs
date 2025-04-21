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
            return MessageType.Ping;
        }
    
        public byte[] Serialize()
        {
            return BitConverter.GetBytes((int)GetMessageType());
        }
    
        public float Deserialize(byte[] message)
        {
            _timestamp = BitConverter.ToSingle(message);
            return _timestamp;
        }
    }
}