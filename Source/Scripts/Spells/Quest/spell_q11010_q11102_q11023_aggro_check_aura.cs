// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 40113 Knockdown Fel Cannon: The Aggro Check Aura
internal class SpellQ11010Q11102Q11023AggroCheckAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleTriggerSpell, 0, AuraType.PeriodicTriggerSpell));
    }

    private void HandleTriggerSpell(AuraEffect aurEff)
    {
        var target = Target;

        if (target)
            // On trigger proccing
            target.SpellFactory.CastSpell(target, QuestSpellIds.AGGRO_CHECK);
    }
}