using System.Net;

namespace Network.ClientDir
{
    public struct Client
    {
        public float LastHeartbeatTime;
        public int id;
        public IPEndPoint ipEndPoint;

        public Client(IPEndPoint ipEndPoint, int id, float timeStamp)
        {
            this.LastHeartbeatTime = timeStamp;
            this.id = id;
            this.ipEndPoint = ipEndPoint;
        }
    }
}