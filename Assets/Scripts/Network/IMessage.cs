using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Network
{
    public enum MessageType
    {
        HandShake = -1,
        Console = 0,
        Position = 1
    }

    public interface IMessage<T>
    {
        public MessageType GetMessageType();
        public byte[] Serialize();
        public T Deserialize(byte[] message);
    }

    public class NetHandShake : IMessage<(long, int)>
    {
        private (long, int) _data;
        public (long, int) Deserialize(byte[] message)
        {
            (long, int) outData;

            outData.Item1 = BitConverter.ToInt64(message, 4);
            outData.Item2 = BitConverter.ToInt32(message, 12);

            return outData;
        }

        public MessageType GetMessageType()
        {
            return MessageType.HandShake;
        }

        public byte[] Serialize()
        {
            List<byte> outData = new List<byte>();

            outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));

            outData.AddRange(BitConverter.GetBytes(_data.Item1));
            outData.AddRange(BitConverter.GetBytes(_data.Item2));


            return outData.ToArray();
        }
    }

    public class NetVector3 : IMessage<Vector3>
    {
        private static ulong _lastMsgID = 0;
        private readonly Vector3 _data;

        public NetVector3()
        {
            _data = new Vector3();
        }
        public NetVector3(Vector3 data)
        {
            this._data = data;
        }

        public Vector3 Deserialize(byte[] message)
        {
            Vector3 outData;

            outData.x = BitConverter.ToSingle(message, 8);
            outData.y = BitConverter.ToSingle(message, 12);
            outData.z = BitConverter.ToSingle(message, 16);

            return outData;
        }

        public MessageType GetMessageType()
        {
            return MessageType.Position;
        }

        public byte[] Serialize()
        {
            List<byte> outData = new List<byte>();

            outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
            outData.AddRange(BitConverter.GetBytes(_lastMsgID++));
            outData.AddRange(BitConverter.GetBytes(_data.x));
            outData.AddRange(BitConverter.GetBytes(_data.y));
            outData.AddRange(BitConverter.GetBytes(_data.z));

            return outData.ToArray();
        }
        
        public byte[] Serialize(Vector3 newData)
        {
            List<byte> outData = new List<byte>();

            outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
            outData.AddRange(BitConverter.GetBytes(_lastMsgID++));
            outData.AddRange(BitConverter.GetBytes(newData.x));
            outData.AddRange(BitConverter.GetBytes(newData.y));
            outData.AddRange(BitConverter.GetBytes(newData.z));

            return outData.ToArray();
        }
        //Dictionary<Client,Dictionary<msgType,int>>
    }
    
    public class NetPlayers : IMessage<Dictionary<int, Vector3>>
    {
        public Dictionary<int, GameObject> Data;

        public NetPlayers()
        {
            Data = new Dictionary<int, GameObject>();
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
    
    public class NetString : IMessage<string>
    {
        public string Data;

        public NetString()
        {
            Data = string.Empty;
        }

        public NetString(string data)
        {
            Data = data;
        }

        public MessageType GetMessageType()
        {
            return MessageType.Console; 
        }

        public byte[] Serialize()
        {
            List<byte> outData = new List<byte>();

            outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
            byte[] stringBytes = Encoding.UTF8.GetBytes(Data);
            outData.AddRange(BitConverter.GetBytes(stringBytes.Length));
            outData.AddRange(stringBytes);

            return outData.ToArray();
        }
        
        public byte[] Serialize(string newData)
        {
            List<byte> outData = new List<byte>();

            outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
            byte[] stringBytes = Encoding.UTF8.GetBytes(newData);
            outData.AddRange(BitConverter.GetBytes(stringBytes.Length));
            outData.AddRange(stringBytes);

            return outData.ToArray();
        }

        public string Deserialize(byte[] message)
        {
            int offset = 4; 
            int stringLength = BitConverter.ToInt32(message, offset);
            offset += 4;

            Data = Encoding.UTF8.GetString(message, offset, stringLength);
            return Data;
        }
    }
}