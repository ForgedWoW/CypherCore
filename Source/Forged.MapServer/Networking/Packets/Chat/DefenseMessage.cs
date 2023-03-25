﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Networking.Packets;

class DefenseMessage : ServerPacket
{
	public uint ZoneID;
	public string MessageText = "";
	public DefenseMessage() : base(ServerOpcodes.DefenseMessage) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(ZoneID);
		_worldPacket.WriteBits(MessageText.GetByteCount(), 12);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(MessageText);
	}
}