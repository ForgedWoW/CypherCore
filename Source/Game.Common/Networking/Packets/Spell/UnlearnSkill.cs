﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Spell;

public class UnlearnSkill : ClientPacket
{
	public uint SkillLine;
	public UnlearnSkill(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SkillLine = _worldPacket.ReadUInt32();
	}
}
