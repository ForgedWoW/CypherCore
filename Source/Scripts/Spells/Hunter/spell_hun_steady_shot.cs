// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[Script]
internal class spell_hun_steady_shot : SpellScript, ISpellOnHit
{
	public override bool Load()
	{
		return Caster.IsTypeId(TypeId.Player);
	}

	public void OnHit()
	{
		Caster.CastSpell(Caster, HunterSpells.SteadyShotFocus, true);
	}
}