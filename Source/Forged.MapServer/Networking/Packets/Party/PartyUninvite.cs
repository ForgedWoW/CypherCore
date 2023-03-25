﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

class PartyUninvite : ClientPacket
{
	public byte PartyIndex;
	public ObjectGuid TargetGUID;
	public string Reason;
	public PartyUninvite(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();
		TargetGUID = _worldPacket.ReadPackedGuid();

		var reasonLen = _worldPacket.ReadBits<byte>(8);
		Reason = _worldPacket.ReadString(reasonLen);
	}
}