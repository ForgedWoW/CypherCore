﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

class TrainerBuySpell : ClientPacket
{
	public ObjectGuid TrainerGUID;
	public uint TrainerID;
	public uint SpellID;
	public TrainerBuySpell(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TrainerGUID = _worldPacket.ReadPackedGuid();
		TrainerID = _worldPacket.ReadUInt32();
		SpellID = _worldPacket.ReadUInt32();
	}
}