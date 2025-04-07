using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Network.Messages
{
    public class NetPlayers : IMessage<Dictionary<int, Vector3>>
    {
        public IReadOnlyDictionary<int, GameObject> Data;

        public NetPlayers()
        {
            Data = new ConcurrentDictionary<int, GameObject>();
        }
        
        public MessageType GetMessageType()
        {
            return MessageType.HandShake; 
        }

        public byte[] Serialize()
        {
            List<byte> outData = new List<byte>();

            outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
            outData.AddRange(BitConverter.GetBytes(Data.Count));

            foreach (KeyValuePair<int, GameObject> kvp in Data)
            {
                outData.AddRange(BitConverter.GetBytes(kvp.Key));
                Vector3 position = kvp.Value.transform.position;
                outData.AddRange(BitConverter.GetBytes(position.x));
                outData.AddRange(BitConverter.GetBytes(position.y));
                outData.AddRange(BitConverter.GetBytes(position.z));
            }

            return outData.ToArray();
        }

        public Dictionary<int, Vector3> Deserialize(byte[] message)
        {
            Dictionary<int, Vector3> outData = new Dictionary<int, Vector3>();

            int offset = 4; // Skip the MessageType
            int count = BitConverter.ToInt32(message, offset);
            offset += 4;

            for (int i = 0; i < count; i++)
            {
                int key = BitConverter.ToInt32(message, offset);
                offset += 4;

                float x = BitConverter.ToSingle(message, offset);
                offset += 4;
                float y = BitConverter.ToSingle(message, offset);
                offset += 4;
                float z = BitConverter.ToSingle(message, offset);
                offset += 4;

                Vector3 position = new Vector3(x, y, z);

                outData[key] = position;
            }

            return outData;
        }
    }
}