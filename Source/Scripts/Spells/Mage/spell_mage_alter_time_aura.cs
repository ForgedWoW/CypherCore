﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 342246 - Alter Time Aura
internal class spell_mage_alter_time_aura : AuraScript, IHasAuraEffects
{
	private ulong _health;
	private Position _pos;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.AlterTimeVisual, MageSpells.MasterOfTime, MageSpells.Blink);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.OverrideActionbarSpells, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.OverrideActionbarSpells, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var unit = GetTarget();
		_health = unit.GetHealth();
		_pos    = new Position(unit.GetPosition());
	}

	private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var unit = GetTarget();

		if (unit.GetDistance(_pos) <= 100.0f &&
		    GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
		{
			unit.SetHealth(_health);
			unit.NearTeleportTo(_pos);

			if (unit.HasAura(MageSpells.MasterOfTime))
			{
				var blink = Global.SpellMgr.GetSpellInfo(MageSpells.Blink, Difficulty.None);
				unit.GetSpellHistory().ResetCharges(blink.ChargeCategoryId);
			}

			unit.CastSpell(unit, MageSpells.AlterTimeVisual);
		}
	}
}