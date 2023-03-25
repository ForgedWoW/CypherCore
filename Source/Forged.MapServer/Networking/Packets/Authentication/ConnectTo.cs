// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Linq;
using System.Security.Cryptography;
using Framework.Constants;
using Framework.Cryptography;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.Authentication;

class ConnectTo : ServerPacket
{
	public enum AddressType
	{
		IPv4 = 1,
		IPv6 = 2,
		NamedSocket = 3 // not supported by windows client
	}

	public ulong Key;
	public ConnectToSerial Serial;
	public ConnectPayload Payload;
	public byte Con;

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
			default:
				break;
		}

		Sha256 hash = new();
		hash.Process(whereBuffer.GetData(), (int)whereBuffer.GetSize());
		hash.Process((uint)Payload.Where.Type);
		hash.Finish(BitConverter.GetBytes(Payload.Port));

		Payload.Signature = RsaCrypt.RSA.SignHash(hash.Digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Reverse().ToArray();

		_worldPacket.WriteBytes(Payload.Signature, (uint)Payload.Signature.Length);
		_worldPacket.WriteBytes(whereBuffer);
		_worldPacket.WriteUInt16(Payload.Port);
		_worldPacket.WriteUInt32((uint)Serial);
		_worldPacket.WriteUInt8(Con);
		_worldPacket.WriteUInt64(Key);
	}

	public class ConnectPayload
	{
		public SocketAddress Where;
		public ushort Port;
		public byte[] Signature = new byte[256];
	}

	public struct SocketAddress
	{
		public AddressType Type;

		public byte[] IPv4;
		public byte[] IPv6;
		public string NameSocket;
	}
}