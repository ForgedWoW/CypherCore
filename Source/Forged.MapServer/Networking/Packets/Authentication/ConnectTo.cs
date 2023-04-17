// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Linq;
using System.Security.Cryptography;
using Framework.Constants;
using Framework.Cryptography;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.Authentication;

internal class ConnectTo : ServerPacket
{
    public enum AddressType
    {
        IPv4 = 1,
        IPv6 = 2,
        NamedSocket = 3 // not supported by windows client
    }

    public byte Con;

    public ulong Key;

    public ConnectPayload Payload;

    public ConnectToSerial Serial;

    public ConnectTo() : base(ServerOpcodes.ConnectTo)
    {
        Payload = new ConnectPayload();
    }

    public override void Write()
    {
        ByteBuffer whereBuffer = new();
        whereBuffer.WriteUInt8((byte)Payload.Where.Type);

        switch (Payload.Where.Type)
        {
            case AddressType.IPv4:
                whereBuffer.WriteBytes(Payload.Where.IPv4);

                break;
            case AddressType.IPv6:
                whereBuffer.WriteBytes(Payload.Where.IPv6);

                break;
            case AddressType.NamedSocket:
                whereBuffer.WriteString(Payload.Where.NameSocket);

                break;
        }

        Sha256 hash = new();
        hash.Process(whereBuffer.GetData(), (int)whereBuffer.GetSize());
        hash.Process((uint)Payload.Where.Type);
        hash.Finish(BitConverter.GetBytes(Payload.Port));

        Payload.Signature = RsaCrypt.RSA.SignHash(hash.Digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Reverse().ToArray();

        WorldPacket.WriteBytes(Payload.Signature, (uint)Payload.Signature.Length);
        WorldPacket.WriteBytes(whereBuffer);
        WorldPacket.WriteUInt16(Payload.Port);
        WorldPacket.WriteUInt32((uint)Serial);
        WorldPacket.WriteUInt8(Con);
        WorldPacket.WriteUInt64(Key);
    }

    public struct SocketAddress
    {
        public byte[] IPv4;
        public byte[] IPv6;
        public string NameSocket;
        public AddressType Type;
    }

    public class ConnectPayload
    {
        public ushort Port;
        public byte[] Signature = new byte[256];
        public SocketAddress Where;
    }
}