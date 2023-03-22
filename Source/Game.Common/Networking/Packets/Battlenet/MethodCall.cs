﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.IO;

namespace Game.Networking.Packets;

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