// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class PartyCommandResult : ServerPacket
{
	public string Name;
	public byte Command;
	public byte Result;
	public uint ResultData;
	public ObjectGuid ResultGUID;
	public PartyCommandResult() : base(ServerOpcodes.PartyCommandResult) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Name.GetByteCount(), 9);
		_worldPacket.WriteBits(Command, 4);
		_worldPacket.WriteBits(Result, 6);

		_worldPacket.WriteUInt32(ResultData);
		_worldPacket.WritePackedGuid(ResultGUID);
		_worldPacket.WriteString(Name);
	}
}

//Structs