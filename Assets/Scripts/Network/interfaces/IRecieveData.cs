using System.Net;

namespace Network.interfaces
{
    public interface IReceiveData
    {
        void OnReceiveData(byte[] data, IPEndPoint ipEndpoint);
    }
}