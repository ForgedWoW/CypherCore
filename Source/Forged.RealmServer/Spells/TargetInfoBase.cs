// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.RealmServer.Spells;

public class TargetInfoBase
{
	public HashSet<int> Effects;

	public virtual void PreprocessTarget(Spell spell) { }
	public virtual void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo) { }
	public virtual void DoDamageAndTriggers(Spell spell) { }
}