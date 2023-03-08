// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(195181)]
public class spell_dk_bone_shield : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
		AuraEffects.Add(new AuraEffectApplyHandler(OnStackChange, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.RealOrReapplyMask));
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> UnnamedParameter2)
	{
		amount.Value = -1;
	}

	private double Absorb(AuraEffect aurEffd, DamageInfo dmgInfo, double absorbAmount)
	{
		absorbAmount = 0;
		var target = Target;

		if (target == null)
			return absorbAmount;

		var absorbPerc = SpellInfo.GetEffect(4).CalcValue(target);
		var absorbStack = 1;

		var aurEff = target.GetAuraEffect(211078, 0);

		if (aurEff != null) // Spectral Deflection
			if (target.CountPctFromMaxHealth(aurEff.Amount) < dmgInfo.GetDamage())
			{
				absorbPerc *= 2;
				absorbStack *= 2;
				ModStackAmount(-1);
			}

		aurEff = target.GetAuraEffect(192558, 0);

		if (aurEff != null) // Skeletal Shattering
		{
			var thisPlayer = target.AsPlayer;

			if (thisPlayer != null)
				if (RandomHelper.randChance(thisPlayer.ActivePlayerData.SpellCritPercentage))
					absorbPerc += aurEff.Amount;
		}

		absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), absorbPerc);

		var _player = target.AsPlayer;

		if (_player != null)
			if ((dmgInfo.GetSchoolMask() & SpellSchoolMask.Normal) != 0)
			{
				//    if (AuraEffect const* aurEff = _player->GetAuraEffect(251876, 0)) // Item - Death Knight T21 Blood 2P Bonus
				//      _player->GetSpellHistory()->ModifyCooldown(49028, aurEff->GetAmount() * absorbStack);
				if (_player.HasSpell(221699)) // Blood Tap
				{
					var spellInfo = Global.SpellMgr.GetSpellInfo(221699, Difficulty.None);

					if (spellInfo != null)
						_player.						SpellHistory.ModifyCooldown(221699, TimeSpan.FromSeconds(1000 * spellInfo.GetEffect(1).CalcValue(target) * absorbStack));
				}

				ModStackAmount(-1);
			}

		return absorbAmount;
	}

	private void OnStackChange(AuraEffect aurEffd, AuraEffectHandleModes UnnamedParameter)
	{
		var target = Target;

		if (target == null)
			return;

		var aurEff = target.GetAuraEffect(219786, 0);

		if (aurEff != null) // Ossuary
		{
			if (StackAmount >= aurEff.Amount)
			{
				if (!target.HasAura(219788))
					target.CastSpell(target, 219788, true);
			}
			else
			{
				target.RemoveAura(219788);
			}
		}
	}
}