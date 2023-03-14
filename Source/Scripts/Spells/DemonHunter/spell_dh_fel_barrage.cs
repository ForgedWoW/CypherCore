// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(211053)]
public class spell_dh_fel_barrage : AuraScript, IHasAuraEffects
{
	private int _charges = 1;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Load()
	{
		var caster = Caster;

		if (caster == null || SpellInfo == null)
			return false;

		var chargeCategoryId = SpellInfo.ChargeCategoryId;

		while (caster.SpellHistory.HasCharge(chargeCategoryId))
		{
			caster.SpellHistory.ConsumeCharge(chargeCategoryId);
			_charges++;
		}

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleTrigger, 0, AuraType.PeriodicDummy));
	}

	private void HandleTrigger(AuraEffect UnnamedParameter)
	{
		var caster = Caster;
		var target = Target;

		if (caster == null || target == null)
			return;

		var args = new CastSpellExtraArgs();
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)_charges);
		args.SetTriggerFlags(TriggerCastFlags.FullMask);
		caster.CastSpell(target, DemonHunterSpells.FEL_BARRAGE_TRIGGER, args);
	}
}