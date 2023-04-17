// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 20711 - Spirit of Redemption
internal class SpellPriSpiritOfRedemption : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0, true, AuraScriptHookType.EffectAbsorb));
    }

    private double HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        var target = Target;
        target.SpellFactory.CastSpell(target, PriestSpells.SPIRIT_OF_REDEMPTION, new CastSpellExtraArgs(aurEff));
        target.SetFullHealth();

        return dmgInfo.Damage;
    }
}