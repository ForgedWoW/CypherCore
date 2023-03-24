// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;

namespace Game.Common.Entities.Creatures;

struct SpellFocusInfo
{
	public Spell Spell;
	public uint Delay;        // ms until the creature's target should snap back (0 = no snapback scheduled)
	public ObjectGuid Target; // the creature's "real" target while casting
	public float Orientation; // the creature's "real" orientation while casting
}
