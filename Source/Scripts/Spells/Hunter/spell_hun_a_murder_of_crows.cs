// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[Script] // 131894 - A Murder of Crows
internal class SpellHunAMurderOfCrows : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
        AuraEffects.Add(new AuraEffectApplyHandler(RemoveEffect, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void HandleDummyTick(AuraEffect aurEff)
    {
        var target = Target;
        var caster = Caster;

        caster?.SpellFactory.CastSpell(target, HunterSpells.AMurderOfCrowsDamage, true);

        target.SpellFactory.CastSpell(target, HunterSpells.A_MURDER_OF_CROWS_VISUAL1, true);
        target.SpellFactory.CastSpell(target, HunterSpells.A_MURDER_OF_CROWS_VISUAL2, true);
        target.SpellFactory.CastSpell(target, HunterSpells.A_MURDER_OF_CROWS_VISUAL3, true);
        target.SpellFactory.CastSpell(target, HunterSpells.A_MURDER_OF_CROWS_VISUAL3, true); // not a mistake, it is intended to cast twice
    }

    private void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (TargetApplication.RemoveMode == AuraRemoveMode.Death)
        {
            var caster = Caster;

            caster?.SpellHistory.ResetCooldown(Id, true);
        }
    }
}