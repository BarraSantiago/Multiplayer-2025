using System;
using System.Collections.Generic;
using Network.Factory;
using UnityEngine;

namespace Network.Messages
{
    public class NetCreateObject : IMessage<NetworkObjectCreateMessage>
    {
        public NetworkObjectCreateMessage data;
        public MessageType GetMessageType()
        {
            throw new System.NotImplementedException();
        }

        public byte[] Serialize()
        {
            List<byte> serializedData = new List<byte>();
            serializedData.AddRange(BitConverter.GetBytes(data.NetworkId));
            serializedData.AddRange(BitConverter.GetBytes((int)data.PrefabType));
            serializedData.AddRange(BitConverter.GetBytes(data.Position.x));
            serializedData.AddRange(BitConverter.GetBytes(data.Position.y));
            serializedData.AddRange(BitConverter.GetBytes(data.Position.z));
            serializedData.AddRange(BitConverter.GetBytes(data.Rotation.x));
            serializedData.AddRange(BitConverter.GetBytes(data.Rotation.y));
            serializedData.AddRange(BitConverter.GetBytes(data.Rotation.z));
            
            return serializedData.ToArray();
        }

        public byte[] Serialize(NetworkObjectCreateMessage newData)
        {
            List<byte> serializedData = new List<byte>();
            serializedData.AddRange(BitConverter.GetBytes(newData.NetworkId));
            serializedData.AddRange(BitConverter.GetBytes((int)newData.PrefabType));
            serializedData.AddRange(BitConverter.GetBytes(newData.Position.x));
            serializedData.AddRange(BitConverter.GetBytes(newData.Position.y));
            serializedData.AddRange(BitConverter.GetBytes(newData.Position.z));
            serializedData.AddRange(BitConverter.GetBytes(newData.Rotation.x));
            serializedData.AddRange(BitConverter.GetBytes(newData.Rotation.y));
            serializedData.AddRange(BitConverter.GetBytes(newData.Rotation.z));
            
            return serializedData.ToArray();
        }
        
        public NetworkObjectCreateMessage Deserialize(byte[] message)
        {
            NetworkObjectCreateMessage newData = new NetworkObjectCreateMessage
            {
                NetworkId = BitConverter.ToInt32(message, 0),
                PrefabType = (NetObjectTypes)BitConverter.ToInt32(message, 4),
                Position = new Vector3(
                    BitConverter.ToSingle(message, 8),
                    BitConverter.ToSingle(message, 12),
                    BitConverter.ToSingle(message, 16)
                ),
                Rotation = new Vector3(
                    BitConverter.ToSingle(message, 20),
                    BitConverter.ToSingle(message, 24),
                    BitConverter.ToSingle(message, 28)
                )
            };

            return newData;
        }
    }
}