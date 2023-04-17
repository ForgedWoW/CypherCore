// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

//146739 - Corruption
[SpellScript(146739)]
public class SpellWarlCorruptionEffect : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
    }

    private void HandleApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var target = Target;
        var caster = Caster;

        if (target == null || caster == null)
            return;

        //If the target is a player, only cast for the time said in ABSOLUTE_CORRUPTION
        if (caster.HasAura(WarlockSpells.ABSOLUTE_CORRUPTION))
            Aura.SetDuration(target.TypeId == TypeId.Player ? Global.SpellMgr.GetSpellInfo(WarlockSpells.ABSOLUTE_CORRUPTION, Difficulty.None).GetEffect(0).BasePoints * Time.IN_MILLISECONDS : 60 * 60 * Time.IN_MILLISECONDS); //If not player, 1 hour
    }

    /*
    Removes the aura if the caster is null, far away or dead.
    */
    private void HandlePeriodic(AuraEffect unnamedParameter)
    {
        var target = Target;
        var caster = Caster;

        if (target == null)
            return;

        if (caster == null)
        {
            target.RemoveAura(WarlockSpells.CORRUPTION_DAMAGE);

            return;
        }

        if (caster.IsDead)
            target.RemoveAura(WarlockSpells.CORRUPTION_DAMAGE);

        if (!caster.IsInRange(target, 0, 80))
            target.RemoveAura(WarlockSpells.CORRUPTION_DAMAGE);
    }
}