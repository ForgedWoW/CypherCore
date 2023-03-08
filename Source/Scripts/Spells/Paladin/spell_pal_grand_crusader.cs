// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

[SpellScript(85043)] // -85043 - Grand Crusader
internal class spell_pal_grand_crusader : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PaladinSpells.AvengersShield);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return Target.IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		Target.GetSpellHistory().ResetCooldown(PaladinSpells.AvengersShield, true);
	}
}