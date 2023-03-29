﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 20711 - Spirit of Redemption
internal class spell_pri_spirit_of_redemption : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0, true, AuraScriptHookType.EffectAbsorb));
    }

    private double HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        var target = Target;
        target.CastSpell(target, PriestSpells.SPIRIT_OF_REDEMPTION, new CastSpellExtraArgs(aurEff));
        target.SetFullHealth();

        return dmgInfo.Damage;
    }
}