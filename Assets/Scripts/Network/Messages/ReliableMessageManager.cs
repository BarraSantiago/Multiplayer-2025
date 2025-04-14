using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;

namespace Network.Messages
{
    public class ReliableMessageManager
    {
        private class PendingMessage
        {
            public byte[] Data;
            public IPEndPoint Target;
            public float FirstSentTime;
            public float LastSentTime;
            public int RetryCount;
        }

        private readonly UdpConnection _connection;
        private readonly Dictionary<uint, PendingMessage> _pendingMessages = new Dictionary<uint, PendingMessage>();
        private readonly object _pendingLock = new object();
        private int _nextSequenceNumber = 1;

        public float ResendInterval = 1.0f;
        public int MaxRetries = 5;

        private HashSet<uint> _receivedMessages = new HashSet<uint>();
        private Dictionary<IPEndPoint, uint> _lastReceivedSequence = new Dictionary<IPEndPoint, uint>();

        public ReliableMessageManager(UdpConnection connection)
        {
            _connection = connection;

            // Start the resend thread
            Thread resendThread = new Thread(ResendLoop)
            {
                IsBackground = true
            };
            resendThread.Start();
        }

        public byte[] PrepareMessage(byte[] messageBody, MessageType type, bool isImportant)
        {
            uint sequenceNumber = GetNextSequenceNumber();

            // Header
            MessageHeader header = new MessageHeader(type, sequenceNumber, isImportant);
            byte[] headerBytes = header.Serialize();

            // Calculate body checksum
            uint bodyChecksum = MessageHeader.CalculateBodyChecksum(messageBody);
            BitConverter.GetBytes(bodyChecksum).CopyTo(headerBytes, 11);

            byte[] fullMessage = new byte[headerBytes.Length + messageBody.Length];
            headerBytes.CopyTo(fullMessage, 0);
            messageBody.CopyTo(fullMessage, headerBytes.Length);

            return fullMessage;
        }

        public void SendMessage(byte[] data, IPEndPoint target, bool isImportant)
        {
            _connection.Send(data, target);

            if (isImportant)
            {
                uint sequenceNumber = BitConverter.ToUInt32(data, 4);

                lock (_pendingLock)
                {
                    _pendingMessages[sequenceNumber] = new PendingMessage
                    {
                        Data = data,
                        Target = target,
                        FirstSentTime = Time.realtimeSinceStartup,
                        LastSentTime = Time.realtimeSinceStartup,
                        RetryCount = 0
                    };
                }
            }
        }

        public void ProcessAcknowledgment(uint sequenceNumber)
        {
            lock (_pendingLock)
            {
                if (_pendingMessages.ContainsKey(sequenceNumber))
                {
                    _pendingMessages.Remove(sequenceNumber);
                    Debug.Log($"Message {sequenceNumber} acknowledged");
                }
            }
        }

        public void SendAcknowledgment(uint sequenceNumber, IPEndPoint target)
        {
            byte[] ackData = new byte[8];
            BitConverter.GetBytes((int)MessageType.Acknowledgment).CopyTo(ackData, 0);
            BitConverter.GetBytes(sequenceNumber).CopyTo(ackData, 4);
            _connection.Send(ackData, target);
        }

        public bool IsNewMessage(uint sequenceNumber, IPEndPoint sender)
        {
            if (!_lastReceivedSequence.TryGetValue(sender, out uint lastSequence))
            {
                _lastReceivedSequence[sender] = sequenceNumber;
                return true;
            }

            if (sequenceNumber > lastSequence)
            {
                _lastReceivedSequence[sender] = sequenceNumber;
                return true;
            }

            if (_receivedMessages.Contains(sequenceNumber)) return false;
            _receivedMessages.Add(sequenceNumber);
            return true;

        }

        private uint GetNextSequenceNumber()
        {
            return (uint)Interlocked.Increment(ref _nextSequenceNumber);
        }

        private void ResendLoop()
        {
            while (true)
            {
                try
                {
                    float currentTime = Time.realtimeSinceStartup;
                    List<uint> toRemove = new List<uint>();

                    lock (_pendingLock)
                    {
                        foreach (var pair in _pendingMessages)
                        {
                            PendingMessage message = pair.Value;

                            // Check if it's time to resend
                            if (currentTime - message.LastSentTime > ResendInterval)
                            {
                                if (message.RetryCount >= MaxRetries)
                                {
                                    Debug.LogWarning(
                                        $"Failed to deliver message {pair.Key} after {MaxRetries} attempts");
                                    toRemove.Add(pair.Key);
                                }
                                else
                                {
                                    // Resend the message
                                    _connection.Send(message.Data, message.Target);
                                    message.LastSentTime = currentTime;
                                    message.RetryCount++;
                                    Debug.Log($"Resending message {pair.Key}, attempt {message.RetryCount}");
                                }
                            }
                        }

                        // Remove expired messages
                        foreach (uint key in toRemove)
                        {
                            _pendingMessages.Remove(key);
                        }
                    }

                    // Clean up old received messages periodically
                    if (_receivedMessages.Count > 10000)
                    {
                        _receivedMessages.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in resend loop: {ex.Message}");
                }

                Thread.Sleep(100);
            }
        }
    }
}