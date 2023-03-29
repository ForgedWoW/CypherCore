﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(219809)]
public class spell_dk_tombstone : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.SchoolAbsorb));
    }


    private void CalcAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        amount.Value = 0;
        var caster = Caster;

        if (caster != null)
        {
            var aura = caster.GetAura(195181);

            if (aura != null)
            {
                int stack = aura.StackAmount;
                var maxStack = (int)SpellInfo.GetEffect(4).CalcValue(caster);

                if (stack > maxStack)
                    stack = maxStack;

                amount.Value = caster.CountPctFromMaxHealth(SpellInfo.GetEffect(3).CalcValue(caster)) * stack;
                var _player = caster.AsPlayer;

                if (_player != null)
                {
                    if (_player.HasSpell(221699)) // Blood Tap
                    {
                        var spellInfo = Global.SpellMgr.GetSpellInfo(221699, Difficulty.None);

                        if (spellInfo != null)
                            _player.CastSpell(_player, 221699, 1000 * spellInfo.GetEffect(1).CalcValue(caster) * stack);
                    }

                    var aurEff = caster.GetAuraEffect(251876, 0); // Item - Death Knight T21 Blood 2P Bonus

                    if (aurEff != null)
                        _player.CastSpell(_player, 49028, aurEff.Amount * stack);

                    aura.ModStackAmount(-1 * stack, AuraRemoveMode.EnemySpell);
                }
            }
        }
    }
}