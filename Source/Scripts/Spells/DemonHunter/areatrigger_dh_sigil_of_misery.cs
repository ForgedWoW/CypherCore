// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script]
internal class areatrigger_dh_sigil_of_misery : AreaTriggerScript, IAreaTriggerOnRemove
{
	public void OnRemove()
	{
		var caster = At.GetCaster();

		caster?.CastSpell(At.Location, DemonHunterSpells.SigilOfMiseryAoe, new CastSpellExtraArgs());
	}
}