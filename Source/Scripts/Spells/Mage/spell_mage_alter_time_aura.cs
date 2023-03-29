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
    private long _health;
    private Position _pos;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.OverrideActionbarSpells, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.OverrideActionbarSpells, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var unit = Target;
        _health = unit.Health;
        _pos = new Position(unit.Location);
    }

    private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var unit = Target;

        if (unit.GetDistance(_pos) <= 100.0f &&
            TargetApplication.RemoveMode == AuraRemoveMode.Expire)
        {
            unit.SetHealth(_health);
            unit.NearTeleportTo(_pos);

            if (unit.HasAura(MageSpells.MasterOfTime))
            {
                var blink = Global.SpellMgr.GetSpellInfo(MageSpells.Blink, Difficulty.None);
                unit.SpellHistory.ResetCharges(blink.ChargeCategoryId);
            }

            unit.CastSpell(unit, MageSpells.AlterTimeVisual);
        }
    }
}