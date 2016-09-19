﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace Telegram.Net.Core.Network
{
    public class MtProtoPlainSender
    {
        private int sequence = 0;
        private int _timeOffset;
        private long _lastMessageId;
        private readonly Random _random;
        private readonly TcpTransport _transport;

        public MtProtoPlainSender(TcpTransport transport)
        {
            _transport = transport;
            _random = new Random();
        }

        public async Task Send(byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write((long)0);
                    binaryWriter.Write(GetNewMessageId());
                    binaryWriter.Write(data.Length);
                    binaryWriter.Write(data);

                    byte[] packet = memoryStream.ToArray();

                    await _transport.Send(packet);
                }
            }
        }

        public async Task<byte[]> Receive()
        {
            var result = await _transport.Receieve();

            using (var memoryStream = new MemoryStream(result.Body))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    long authKeyid = binaryReader.ReadInt64();
                    long messageId = binaryReader.ReadInt64();
                    int messageLength = binaryReader.ReadInt32();

                    byte[] response = binaryReader.ReadBytes(messageLength);

                    return response;
                }
            }
        }

        private long GetNewMessageId()
        {
            long time = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);
            long newMessageId = ((time / 1000 + _timeOffset) << 32) |
                                ((time % 1000) << 22) |
                                (_random.Next(524288) << 2); // 2^19
                                                            // [ unix timestamp : 32 bit] [ milliseconds : 10 bit ] [ buffer space : 1 bit ] [ random : 19 bit ] [ msg_id type : 2 bit ] = [ msg_id : 64 bit ]

            if (_lastMessageId >= newMessageId)
            {
                newMessageId = _lastMessageId + 4;
            }

            _lastMessageId = newMessageId;
            return newMessageId;
        }


    }
}