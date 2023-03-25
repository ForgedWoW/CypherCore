// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells;

public class SpellThreatEntry
{
	public int FlatMod;    // flat threat-value for this Spell  - default: 0
	public float PctMod;   // threat-multiplier for this Spell  - default: 1.0f
	public float ApPctMod; // Pct of AP that is added as Threat - default: 0.0f
}