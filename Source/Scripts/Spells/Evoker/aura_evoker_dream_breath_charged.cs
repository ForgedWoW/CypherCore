// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.DREAM_BREATH_CHARGED)]
internal class aura_evoker_dream_breath_charged : AuraScript, IAuraOnApply
{
    public void AuraApplied()
	{
		var aur = Aura;

		switch (aur.EmpoweredStage)
		{
			case 1:
				aur.SetMaxDuration(12000);
				aur.SetDuration(12000, true);

				break;
			case 2:
				aur.SetMaxDuration(8000);
				aur.SetDuration(8000, true);

				break;
			case 3:
				aur.SetMaxDuration(4000);
				aur.SetDuration(4000, true);

				break;
			default:
				aur.SetMaxDuration(16000);
				aur.SetDuration(16000, true);

				break;
		}
	}
}