// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 40438 - Priest Tier 6 Trinket
internal class SpellPriItemT6Trinket : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var caster = eventInfo.Actor;

        if (eventInfo.SpellTypeMask.HasAnyFlag(ProcFlagsSpellType.Heal))
            caster.SpellFactory.CastSpell((Unit)null, PriestSpells.DIVINE_BLESSING, true);

        if (eventInfo.SpellTypeMask.HasAnyFlag(ProcFlagsSpellType.Damage))
            caster.SpellFactory.CastSpell((Unit)null, PriestSpells.DIVINE_WRATH, true);
    }
}