// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior;

[Script] // 32215 - Victorious State
internal class spell_warr_victorious_state : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(WarriorSpells.IMPENDING_VICTORY);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleOnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleOnProc(AuraEffect aurEff, ProcEventInfo procInfo)
	{
		if (procInfo.Actor.TypeId == TypeId.Player &&
			procInfo.Actor.AsPlayer.GetPrimarySpecialization() == TalentSpecialization.WarriorFury)
			PreventDefaultAction();

		procInfo.Actor.
		SpellHistory.ResetCooldown(WarriorSpells.IMPENDING_VICTORY, true);
	}
}