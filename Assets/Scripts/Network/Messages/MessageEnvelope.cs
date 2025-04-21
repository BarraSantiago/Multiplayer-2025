using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Network.Messages
{
    public class MessageEnvelope
    {
        public bool IsCritical { get; set; }
        public MessageType MessageType { get; set; }
        public int MessageNumber { get; set; }
        public bool IsImportant { get; set; }
        public byte[] Data { get; set; }

        public int Checksum1 { get; private set; }
        public int Checksum2 { get; private set; }

        public byte[] Serialize()
        {
            List<byte> result = new List<byte>();
            result.Add(BitConverter.GetBytes(IsCritical ? (byte)1 : (byte)0)[0]);
            result.AddRange(BitConverter.GetBytes((int)MessageType));
            result.AddRange(BitConverter.GetBytes(MessageNumber));
            result.Add(BitConverter.GetBytes(IsImportant ? (byte)1 : (byte)0)[0]);

            if (Data != null)
            {
                byte[] dataToAdd = IsCritical ? EncryptData(Data) : Data;
                result.AddRange(dataToAdd);
            }

            CalculateChecksums();
            result.AddRange(BitConverter.GetBytes(Checksum1));
            result.AddRange(BitConverter.GetBytes(Checksum2));

            return result.ToArray();
        }

        public static MessageEnvelope Deserialize(byte[] data)
        {
            MessageEnvelope envelope = new MessageEnvelope();

            int offset = 0;
            envelope.IsCritical = data[offset] == 1;
            offset += 1;

            envelope.MessageType = (MessageType)BitConverter.ToInt32(data, offset);
            offset += 4;

            envelope.MessageNumber = BitConverter.ToInt32(data, offset);
            offset += 4;

            envelope.IsImportant = data[offset] == 1;
            offset += 1;

            // Check if there's data to extract (beside the checksums)
            int dataLength = data.Length - offset - 8;
            if (dataLength > 0)
            {
                byte[] messageData = new byte[dataLength];
                Array.Copy(data, offset, messageData, 0, dataLength);
                envelope.Data = envelope.IsCritical ? DecryptData(messageData) : messageData;
                offset += dataLength;
            }
            else
            {
                envelope.Data = null;
            }

            envelope.Checksum1 = BitConverter.ToInt32(data, offset);
            offset += 4;
            envelope.Checksum2 = BitConverter.ToInt32(data, offset);

            int calculatedChecksum1, calculatedChecksum2;
            envelope.CalculateChecksums(out calculatedChecksum1, out calculatedChecksum2);

            if (calculatedChecksum1 != envelope.Checksum1 || calculatedChecksum2 != envelope.Checksum2)
            {
                throw new Exception("Checksum verification failed");
            }

            return envelope;
        }

        private void CalculateChecksums()
        {
            CalculateChecksums(out int checksum, out int checksum2);
            Checksum1 = checksum;
            Checksum2 = checksum2;
        }

        private void CalculateChecksums(out int checksum1, out int checksum2)
        {
            uint uChecksum1 = 0;
            uint uChecksum2 = 0x12345678; // Starting seed

            byte[] headerData = new byte[10];
            headerData[0] = (byte)(IsCritical ? 1 : 0);
            Array.Copy(BitConverter.GetBytes((int)MessageType), 0, headerData, 1, 4);
            Array.Copy(BitConverter.GetBytes(MessageNumber), 0, headerData, 5, 4);
            headerData[9] = (byte)(IsImportant ? 1 : 0);

            // Checksum 1: XOR and rotate bits using unsigned int
            for (int i = 0; i < headerData.Length; i++)
            {
                uChecksum1 ^= (uint)headerData[i] << (8 * (i % 4));
                uChecksum1 = (uChecksum1 << 1) | (uChecksum1 >> 31);
            }

            // Only process Data if it's not null
            if (Data != null)
            {
                for (int i = 0; i < Data.Length; i++)
                {
                    uChecksum1 ^= (uint)Data[i] << (8 * (i % 4));
                    uChecksum1 = (uChecksum1 << 1) | (uChecksum1 >> 31);
                }
            }

            // Checksum 2: Different algorithm
            for (int i = 0; i < headerData.Length; i++)
            {
                uChecksum2 = uChecksum2 * 33 + headerData[i];
            }

            // Only process Data if it's not null
            if (Data != null)
            {
                for (int i = 0; i < Data.Length; i++)
                {
                    uChecksum2 = uChecksum2 * 33 + Data[i];
                }
            }

            uChecksum2 ^= uChecksum2 >> 16;

            // Convert back to int
            checksum1 = (int)uChecksum1;
            checksum2 = (int)uChecksum2;
        }

        private byte[] EncryptData(byte[] data)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] key = sha256.ComputeHash(Encoding.UTF8.GetBytes("SecretKey"));

            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using MemoryStream ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);

            using CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        private static byte[] DecryptData(byte[] encryptedData)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] key = sha256.ComputeHash(Encoding.UTF8.GetBytes("SecretKey"));

            using Aes aes = Aes.Create();
            aes.Key = key;

            byte[] iv = new byte[16]; // AES block size
            Array.Copy(encryptedData, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(encryptedData, iv.Length, encryptedData.Length - iv.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }
    }
}