// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(25914)] // 25914 - Holy Shock
internal class spell_pal_holy_shock_heal_visual : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		Caster.SendPlaySpellVisual(HitUnit, IsHitCrit ? SpellVisual.HolyShockHealCrit : SpellVisual.HolyShockHeal, 0, 0, 0.0f, false);
	}
}