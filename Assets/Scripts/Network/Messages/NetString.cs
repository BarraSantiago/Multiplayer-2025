using System;
using System.Collections.Generic;
using System.Text;

namespace Network.Messages
{
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