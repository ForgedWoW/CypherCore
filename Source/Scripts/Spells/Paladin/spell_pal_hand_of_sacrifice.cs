// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;

namespace Scripts.Spells.Paladin;

[SpellScript(6940)] // 6940 - Hand of Sacrifice
internal class SpellPalHandOfSacrifice : AuraScript, IHasAuraEffects
{
    private int _remainingAmount;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        var caster = Caster;

        if (caster)
        {
            _remainingAmount = (int)caster.MaxHealth;

            return true;
        }

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectSplitHandler(Split, 0));
    }

    private double Split(AuraEffect aurEff, DamageInfo dmgInfo, double splitAmount)
    {
        _remainingAmount -= (int)splitAmount;

        if (_remainingAmount <= 0)
            Target.RemoveAura(PaladinSpells.HAND_OF_SACRIFICE);

        return splitAmount;
    }
}