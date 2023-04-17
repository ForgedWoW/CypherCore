// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(81262)]
public class SpellDruEfflorescenceAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleHeal, 0, AuraType.PeriodicDummy));
    }

    private void HandleHeal(AuraEffect unnamedParameter)
    {
        if (Caster && Caster.OwnerUnit)
        {
            Caster.OwnerUnit.SpellFactory.CastSpell(Caster.Location, EfflorescenceSpells.EfflorescenceHeal);

            var playerList = Caster.GetPlayerListInGrid(11.2f);

            foreach (var targets in playerList)
                if (Caster.OwnerUnit.HasAura(DruidSpells.SpringBlossoms))
                    if (!targets.HasAura(DruidSpells.SpringBlossomsHeal))
                        Caster.OwnerUnit.SpellFactory.CastSpell(targets, DruidSpells.SpringBlossomsHeal, true);
        }
    }
}