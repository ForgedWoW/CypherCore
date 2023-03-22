﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Spells;

public class ItemTargetInfo : TargetInfoBase
{
	public Item TargetItem;

	public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
	{
		spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

		spell.HandleEffects(null, TargetItem, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);

		spell.CallScriptOnHitHandlers();
		spell.CallScriptAfterHitHandlers();
	}
}