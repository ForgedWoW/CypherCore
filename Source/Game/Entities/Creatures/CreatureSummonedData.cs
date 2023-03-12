// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Entities;

public class CreatureSummonedData
{
	public uint? CreatureIdVisibleToSummoner { get; set; }
	public uint? GroundMountDisplayId { get; set; }
	public uint? FlyingMountDisplayId { get; set; }
}