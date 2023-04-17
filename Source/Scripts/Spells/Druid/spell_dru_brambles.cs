// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 203953 - Brambles - BRAMBLES_PASSIVE
internal class SpellDruBrambles : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0, false, AuraScriptHookType.EffectAbsorb));
        AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAfterAbsorb, 0, false, AuraScriptHookType.EffectAfterAbsorb));
    }

    private double HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        // Prevent Removal
        PreventDefaultAction();

        return absorbAmount;
    }

    private double HandleAfterAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        // reflect back Damage to the Attacker
        var target = Target;
        var attacker = dmgInfo.Attacker;

        if (attacker != null)
            target.SpellFactory.CastSpell(attacker, DruidSpellIds.BramblesRelect, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorbAmount));

        return absorbAmount;
    }
}