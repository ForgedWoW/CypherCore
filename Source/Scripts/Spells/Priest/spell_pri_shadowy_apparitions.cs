// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(78203)]
public class spell_pri_shadowy_apparitions : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo.Id == PriestSpells.SHADOW_WORD_PAIN)
            if ((eventInfo.HitMask & ProcFlagsHit.Critical) != 0)
                return true;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
    {
        if (Target && eventInfo.ActionTarget)
        {
            Target.CastSpell(eventInfo.ActionTarget, PriestSpells.SHADOWY_APPARITION_MISSILE, true);
            Target.SendPlaySpellVisual(eventInfo.ActionTarget.Location, Caster.Location.Orientation, MiscSpells.VISUAL_SHADOWY_APPARITION, 0, 0, MiscSpells.SHADOWY_APPARITION_TRAVEL_SPEED, false);
        }
    }
}