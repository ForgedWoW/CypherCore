// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Entities;

public class ResurrectionData
{
	public ObjectGuid Guid { get; set; }
	public WorldLocation Location { get; set; } = new();
	public uint Health { get; set; }
	public uint Mana { get; set; }
	public uint Aura { get; set; }
}