// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

[SpellScript(53651)] // 53651 - Beacon of Light
internal class spell_pal_light_s_beacon : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PaladinSpells.BeaconOfLight, PaladinSpells.BeaconOfLightHeal);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (!eventInfo.ActionTarget)
			return false;

		if (eventInfo.ActionTarget.HasAura(PaladinSpells.BeaconOfLight, eventInfo.Actor.GUID))
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var healInfo = eventInfo.HealInfo;

		if (healInfo == null ||
			healInfo.GetHeal() == 0)
			return;

		var heal = MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.Amount);

		var auras = Caster.GetSingleCastAuras();

		foreach (var eff in auras)
			if (eff.Id == PaladinSpells.BeaconOfLight)
			{
				var applications = eff.GetApplicationList();

				if (!applications.Empty())
				{
					CastSpellExtraArgs args = new(aurEff);
					args.AddSpellMod(SpellValueMod.BasePoint0, (int)heal);
					eventInfo.Actor.CastSpell(applications[0].Target, PaladinSpells.BeaconOfLightHeal, args);
				}

				return;
			}
	}
}