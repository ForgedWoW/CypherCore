// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 67228 - Item - Shaman T9 Elemental 4P Bonus (Lava Burst)
[SpellScript(67228)]
internal class spell_sha_t9_elemental_4p_bonus : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.LavaBurstBonusDamage);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var damageInfo = eventInfo.DamageInfo;

		if (damageInfo == null ||
			damageInfo.Damage == 0)
			return;

		var spellInfo = Global.SpellMgr.GetSpellInfo(ShamanSpells.LavaBurstBonusDamage, CastDifficulty);
		var amount = (int)MathFunctions.CalculatePct(damageInfo.Damage, aurEff.Amount);
		amount /= (int)spellInfo.MaxTicks;

		var caster = eventInfo.Actor;
		var target = eventInfo.ProcTarget;

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, amount);
		caster.CastSpell(target, ShamanSpells.LavaBurstBonusDamage, args);
	}
}