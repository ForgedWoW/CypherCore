// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(81262)]
public class spell_dru_efflorescence_aura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleHeal, 0, AuraType.PeriodicDummy));
    }

    private void HandleHeal(AuraEffect UnnamedParameter)
    {
        if (Caster && Caster.OwnerUnit)
        {
            Caster.OwnerUnit.CastSpell(Caster.Location, EfflorescenceSpells.EFFLORESCENCE_HEAL);

            var playerList = Caster.GetPlayerListInGrid(11.2f);

            foreach (var targets in playerList)
                if (Caster.OwnerUnit.HasAura(DruidSpells.SPRING_BLOSSOMS))
                    if (!targets.HasAura(DruidSpells.SPRING_BLOSSOMS_HEAL))
                        Caster.OwnerUnit.CastSpell(targets, DruidSpells.SPRING_BLOSSOMS_HEAL, true);
        }
    }
}