// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 33510 - Health Restore
internal class SpellItemMarkOfConquest : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        if (eventInfo.TypeMask.HasFlag(ProcFlags.DealRangedAttack | ProcFlags.DealRangedAbility))
        {
            // in that case, do not cast heal spell
            PreventDefaultAction();

            // but mana instead
            eventInfo. // but mana instead
                Actor.SpellFactory.CastSpell((Unit)null, ItemSpellIds.MARK_OF_CONQUEST_ENERGIZE, new CastSpellExtraArgs(aurEff));
        }
    }
}