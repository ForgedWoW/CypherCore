// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(390270)]
public class spell_dk_coil_of_devastation : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.DamageInfo != null)
			return eventInfo.DamageInfo.SpellInfo.Id == DeathKnightSpells.DEATH_COIL_DAMAGE;

		return false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		// TODO: This doesn't seem to actually do damage
		PreventDefaultAction();
		var devDot = Global.SpellMgr.GetSpellInfo(DeathKnightSpells.DEATH_COIL_DEVASTATION_DOT);
		var pct = aurEff.Amount;
		var amount = (int)(MathFunctions.CalculatePct(eventInfo.DamageInfo.Damage, pct) / devDot.MaxTicks);

		var args = new CastSpellExtraArgs(aurEff);
		args.SpellValueOverrides[SpellValueMod.BasePoint0] = amount;
		Target.CastSpell(eventInfo.ProcTarget, DeathKnightSpells.DEATH_COIL_DEVASTATION_DOT, args);
	}
}