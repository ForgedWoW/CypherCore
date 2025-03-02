﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_running_wild : SpellScript
{
	public override bool Load()
	{
		// Definitely not a good thing, but currently the only way to do something at cast start
		// Should be replaced as soon as possible with a new hook: BeforeCastStart
		Caster.CastSpell(Caster, GenericSpellIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

		return false;
	}

	public override void Register() { }
}