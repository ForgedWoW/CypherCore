// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script]
internal class spell_mage_cauterize_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.Effects.Count > 2 && ValidateSpellInfo(MageSpells.CauterizeDot, MageSpells.Cauterized, spellInfo.GetEffect(2).TriggerSpell);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0, false, AuraScriptHookType.EffectAbsorb));
	}

	private double HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
	{
		var effectInfo = GetEffect(1);

		if (effectInfo == null ||
			!TargetApplication.HasEffect(1) ||
			dmgInfo.Damage < Target.Health ||
			dmgInfo.Damage > Target.MaxHealth * 2 ||
			Target.HasAura(MageSpells.Cauterized))
		{
			PreventDefaultAction();

			return absorbAmount;
		}

		Target.SetHealth(Target.CountPctFromMaxHealth(effectInfo.Amount));
		Target.CastSpell(Target, GetEffectInfo(2).TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
		Target.CastSpell(Target, MageSpells.CauterizeDot, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
		Target.CastSpell(Target, MageSpells.Cauterized, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

		return absorbAmount;
	}
}