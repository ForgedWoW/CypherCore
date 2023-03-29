﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(45243)]
public class spell_pri_focused_will : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(PreventAction, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private bool HandleProc(ProcEventInfo eventInfo)
    {
        var caster = Caster;

        if (caster == null)
            return false;

        if (eventInfo.DamageInfo.AttackType == WeaponAttackType.BaseAttack || eventInfo.DamageInfo.AttackType == WeaponAttackType.OffAttack)
        {
            caster.CastSpell(caster, PriestSpells.FOCUSED_WILL_BUFF, true);

            return true;
        }

        return false;
    }

    private void PreventAction(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
    {
        PreventDefaultAction();
    }
}