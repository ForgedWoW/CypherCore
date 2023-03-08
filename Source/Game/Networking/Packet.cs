﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.IO;
using Game.Entities;

namespace Game.Networking;

public abstract class ClientPacket : IDisposable
{
	protected WorldPacket _worldPacket;

	protected ClientPacket(WorldPacket worldPacket)
	{
		_worldPacket = worldPacket;
	}

	public void Dispose()
	{
		_worldPacket.Dispose();
	}

	public abstract void Read();

	public ClientOpcodes GetOpcode()
	{
		return (ClientOpcodes)_worldPacket.GetOpcode();
	}

	public void LogPacket(WorldSession session)
	{
		Log.outDebug(LogFilter.Network, "Received ClientOpcode: {0} From: {1}", GetOpcode(), session != null ? session.GetPlayerInfo() : "Unknown IP");
	}
}

public abstract class ServerPacket
{
	protected WorldPacket _worldPacket;
	readonly ConnectionType connectionType;

	byte[] buffer;

	protected ServerPacket(ServerOpcodes opcode)
	{
		connectionType = ConnectionType.Realm;
		_worldPacket = new WorldPacket(opcode);
	}

	protected ServerPacket(ServerOpcodes opcode, ConnectionType type = ConnectionType.Realm)
	{
		connectionType = type;
		_worldPacket = new WorldPacket(opcode);
	}

	public void Clear()
	{
		_worldPacket.Clear();
		buffer = null;
	}

	public ServerOpcodes GetOpcode()
	{
		return (ServerOpcodes)_worldPacket.GetOpcode();
	}

	public byte[] GetData()
	{
		return buffer;
	}

	public void LogPacket(WorldSession session)
	{
		Log.outDebug(LogFilter.Network, "Sent ServerOpcode: {0} To: {1}", GetOpcode(), session != null ? session.GetPlayerInfo() : "");
	}

	public abstract void Write();

	public void WritePacketData()
	{
		if (buffer != null)
			return;

		Write();

		buffer = _worldPacket.GetData();
		_worldPacket.Dispose();
	}

	public ConnectionType GetConnection()
	{
		return connectionType;
	}
}

public class WorldPacket : ByteBuffer
{
	readonly uint opcode;
	DateTime m_receivedTime; // only set for a specific set of opcodes, for performance reasons.

	public WorldPacket(ServerOpcodes opcode = ServerOpcodes.None)
	{
		this.opcode = (uint)opcode;
	}

	public WorldPacket(byte[] data) : base(data)
	{
		opcode = ReadUInt16();
	}

	public ObjectGuid ReadPackedGuid()
	{
		var loLength = ReadUInt8();
		var hiLength = ReadUInt8();
		var low = ReadPackedUInt64(loLength);

		return new ObjectGuid(ReadPackedUInt64(hiLength), low);
	}

	public Position ReadPosition()
	{
		return new Position(ReadFloat(), ReadFloat(), ReadFloat());
	}

	public void Write(ObjectGuid guid)
	{
		WritePackedGuid(guid);
	}

	public void WritePackedGuid(ObjectGuid guid)
	{
		if (guid.IsEmpty)
		{
			WriteUInt8(0);
			WriteUInt8(0);

			return;
		}


		var loSize = PackUInt64(guid.LowValue, out var lowMask, out var lowPacked);
		var hiSize = PackUInt64(guid.HighValue, out var highMask, out var highPacked);

		WriteUInt8(lowMask);
		WriteUInt8(highMask);
		WriteBytes(lowPacked, loSize);
		WriteBytes(highPacked, hiSize);
	}

	public void WritePackedUInt64(ulong guid)
	{
		var packedSize = PackUInt64(guid, out var mask, out var packed);

		WriteUInt8(mask);
		WriteBytes(packed, packedSize);
	}

	public void WriteBytes(WorldPacket data)
	{
		FlushBits();
		WriteBytes(data.GetData());
	}

	public void WriteXYZ(Position pos)
	{
		if (pos == null)
			return;

		WriteFloat(pos.X);
		WriteFloat(pos.Y);
		WriteFloat(pos.Z);
	}

	public void WriteXYZO(Position pos)
	{
		WriteFloat(pos.X);
		WriteFloat(pos.Y);
		WriteFloat(pos.Z);
		WriteFloat(pos.Orientation);
	}

	public uint GetOpcode()
	{
		return opcode;
	}

	public DateTime GetReceivedTime()
	{
		return m_receivedTime;
	}

	public void SetReceiveTime(DateTime receivedTime)
	{
		m_receivedTime = receivedTime;
	}

	private ulong ReadPackedUInt64(byte length)
	{
		if (length == 0)
			return 0;

		var guid = 0ul;

		for (var i = 0; i < 8; i++)
			if ((1 << i & length) != 0)
				guid |= (ulong)ReadUInt8() << (i * 8);

		return guid;
	}

	uint PackUInt64(ulong value, out byte mask, out byte[] result)
	{
		uint resultSize = 0;
		mask = 0;
		result = new byte[8];

		for (byte i = 0; value != 0; ++i)
		{
			if ((value & 0xFF) != 0)
			{
				mask |= (byte)(1 << i);
				result[resultSize++] = (byte)(value & 0xFF);
			}

			value >>= 8;
		}

		return resultSize;
	}
}

public class PacketHeader
{
	public int Size;
	public byte[] Tag = new byte[12];

	public void Read(byte[] buffer)
	{
		Size = BitConverter.ToInt32(buffer, 0);
		Buffer.BlockCopy(buffer, 4, Tag, 0, 12);
	}

	public void Write(ByteBuffer byteBuffer)
	{
		byteBuffer.WriteInt32(Size);
		byteBuffer.WriteBytes(Tag, 12);
	}

	public bool IsValidSize()
	{
		return Size < 0x40000;
	}
}