// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_artifical_stamina : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.Effects.Count > 1;
	}

	public override bool Load()
	{
		return Owner.IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModTotalStatPercentage));
	}

	private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		var artifact = Owner.ToPlayer().GetItemByGuid(Aura.CastItemGuid);

		if (artifact)
			amount.Value = (GetEffectInfo(1).BasePoints * artifact.GetTotalPurchasedArtifactPowers() / 100);
	}
}