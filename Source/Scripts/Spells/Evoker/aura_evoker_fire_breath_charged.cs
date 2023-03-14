// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.FIRE_BREATH_CHARGED)]
internal class aura_evoker_fire_breath_charged : AuraScript, IAuraOnApply
{
	public void AuraApply()
	{
		var aur = Aura;

		switch (aur.EmpoweredStage)
		{
			case 1:
				aur.ModDuration(12000, true, true);

				break;
			case 2:
				aur.ModDuration(6000, true, true);

				break;
			default:
				aur.ModDuration(18000, true, true);

				break;
		}
	}
}