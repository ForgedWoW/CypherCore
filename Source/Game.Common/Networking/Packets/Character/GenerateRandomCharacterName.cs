﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class GenerateRandomCharacterName : ClientPacket
{
	public byte Sex;
	public byte Race;
	public GenerateRandomCharacterName(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Race = _worldPacket.ReadUInt8();
		Sex = _worldPacket.ReadUInt8();
	}
}