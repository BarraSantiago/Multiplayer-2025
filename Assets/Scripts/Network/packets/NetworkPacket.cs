using System.Net;

namespace Network.packets
{
    public enum PacketType
    {
        HandShake,
        HandShake_OK, 
        Error,
        Ping,
        Pong,
        Message,
    }


    public class NetworkPacket
    {
        public PacketType Type;
        public int ClientId;
        public IPEndPoint IPEndPoint;
        public float TimeStamp;
        public byte[] Payload;

        public NetworkPacket(PacketType type, byte[] data, float timeStamp, int clientId = -1, IPEndPoint ipEndPoint = null)
        {
            this.Type = type;
            this.TimeStamp = timeStamp;
            this.ClientId = clientId;
            this.IPEndPoint = ipEndPoint;
            this.Payload = data;
        }
    }
}