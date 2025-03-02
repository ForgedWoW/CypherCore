﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 205021 - Ray of Frost
internal class spell_mage_ray_of_frost : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = Caster;

		caster?.CastSpell(caster, MageSpells.RayOfFrostFingersOfFrost, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
	}
}