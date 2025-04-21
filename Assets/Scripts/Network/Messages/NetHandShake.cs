using System;
using System.Collections.Generic;

namespace Network.Messages
{
    public class NetHandShake : IMessage<(long, int)>
    {
        private (long, int) _data;

        public (long, int) Deserialize(byte[] message)
        {
            (long, int) outData;

            outData.Item1 = BitConverter.ToInt64(message, 0);
            outData.Item2 = BitConverter.ToInt32(message, 8);

            return outData;
        }

        public MessageType GetMessageType()
        {
            return MessageType.HandShake;
        }

        public byte[] Serialize()
        {
            List<byte> outData = new List<byte>();

            outData.AddRange(BitConverter.GetBytes(_data.Item1));
            outData.AddRange(BitConverter.GetBytes(_data.Item2));

            return outData.ToArray();
        }
    }
}