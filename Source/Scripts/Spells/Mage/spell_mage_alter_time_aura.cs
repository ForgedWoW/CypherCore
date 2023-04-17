// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 342246 - Alter Time Aura
internal class SpellMageAlterTimeAura : AuraScript, IHasAuraEffects
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

            if (unit.HasAura(MageSpells.MASTER_OF_TIME))
            {
                var blink = Global.SpellMgr.GetSpellInfo(MageSpells.BLINK, Difficulty.None);
                unit.SpellHistory.ResetCharges(blink.ChargeCategoryId);
            }

            unit.SpellFactory.CastSpell(unit, MageSpells.ALTER_TIME_VISUAL);
        }
    }
}