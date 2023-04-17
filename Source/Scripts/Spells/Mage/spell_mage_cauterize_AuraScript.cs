// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script]
internal class SpellMageCauterizeAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


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
            Target.HasAura(MageSpells.CAUTERIZED))
        {
            PreventDefaultAction();

            return absorbAmount;
        }

        Target.SetHealth(Target.CountPctFromMaxHealth(effectInfo.Amount));
        Target.SpellFactory.CastSpell(Target, GetEffectInfo(2).TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        Target.SpellFactory.CastSpell(Target, MageSpells.CAUTERIZE_DOT, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        Target.SpellFactory.CastSpell(Target, MageSpells.CAUTERIZED, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

        return absorbAmount;
    }
}