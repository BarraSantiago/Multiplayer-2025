namespace Network.Messages
{
    public enum MessageType
    {
        None = -2,
        HandShake = -1,
        Console = 0,
        Position = 1,
        Ping,
        Id
    }

    public interface IMessage<T>
    {
        public MessageType GetMessageType();
        public byte[] Serialize();
        public T Deserialize(byte[] message);
    }
}