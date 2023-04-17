// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 40349 - Corrupting Plague
internal class SpellCorruptingPlagueAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
    }

    private void OnPeriodic(AuraEffect aurEff)
    {
        var owner = Target;

        List<Creature> targets = new();
        CorruptingPlagueSearcher creatureCheck = new(owner, 15.0f);
        CreatureListSearcher creatureSearcher = new(owner, targets, creatureCheck, GridType.Grid);
        Cell.VisitGrid(owner, creatureSearcher, 15.0f);

        if (!targets.Empty())
            return;

        PreventDefaultAction();
    }
}