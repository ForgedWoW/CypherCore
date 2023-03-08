// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.IO;

namespace Game.Networking.Packets;

class Notification : ServerPacket
{
	public MethodCall Method;
	public ByteBuffer Data = new();
	public Notification() : base(ServerOpcodes.BattlenetNotification) { }

	public override void Write()
	{
		Method.Write(_worldPacket);
		_worldPacket.WriteUInt32(Data.GetSize());
		_worldPacket.WriteBytes(Data);
	}
}

class Response : ServerPacket
{
	public BattlenetRpcErrorCode BnetStatus = BattlenetRpcErrorCode.Ok;
	public MethodCall Method;
	public ByteBuffer Data = new();
	public Response() : base(ServerOpcodes.BattlenetResponse) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)BnetStatus);
		Method.Write(_worldPacket);
		_worldPacket.WriteUInt32(Data.GetSize());
		_worldPacket.WriteBytes(Data);
	}
}

class ConnectionStatus : ServerPacket
{
	public byte State;
	public bool SuppressNotification = true;
	public ConnectionStatus() : base(ServerOpcodes.BattleNetConnectionStatus) { }

	public override void Write()
	{
		_worldPacket.WriteBits(State, 2);
		_worldPacket.WriteBit(SuppressNotification);
		_worldPacket.FlushBits();
	}
}

class ChangeRealmTicketResponse : ServerPacket
{
	public uint Token;
	public bool Allow = true;
	public ByteBuffer Ticket;
	public ChangeRealmTicketResponse() : base(ServerOpcodes.ChangeRealmTicketResponse) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Token);
		_worldPacket.WriteBit(Allow);
		_worldPacket.WriteUInt32(Ticket.GetSize());
		_worldPacket.WriteBytes(Ticket);
	}
}

class BattlenetRequest : ClientPacket
{
	public MethodCall Method;
	public byte[] Data;
	public BattlenetRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Method.Read(_worldPacket);
		var protoSize = _worldPacket.ReadUInt32();

		Data = _worldPacket.ReadBytes(protoSize);
	}
}

class ChangeRealmTicket : ClientPacket
{
	public uint Token;
	public Array<byte> Secret = new(32);
	public ChangeRealmTicket(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Token = _worldPacket.ReadUInt32();

		for (var i = 0; i < Secret.GetLimit(); ++i)
			Secret[i] = _worldPacket.ReadUInt8();
	}
}

public struct MethodCall
{
	public uint GetServiceHash()
	{
		return (uint)(Type >> 32);
	}

	public uint GetMethodId()
	{
		return (uint)(Type & 0xFFFFFFFF);
	}

	public void Read(ByteBuffer data)
	{
		Type = data.ReadUInt64();
		ObjectId = data.ReadUInt64();
		Token = data.ReadUInt32();
	}

	public void Write(ByteBuffer data)
	{
		data.WriteUInt64(Type);
		data.WriteUInt64(ObjectId);
		data.WriteUInt32(Token);
	}

	public ulong Type;
	public ulong ObjectId;
	public uint Token;
}