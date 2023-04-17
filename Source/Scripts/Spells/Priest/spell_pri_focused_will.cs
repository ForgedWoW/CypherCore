// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(45243)]
public class SpellPriFocusedWill : AuraScript, IHasAuraEffects
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
            caster.SpellFactory.CastSpell(caster, PriestSpells.FOCUSED_WILL_BUFF, true);

            return true;
        }

        return false;
    }

    private void PreventAction(AuraEffect unnamedParameter, ProcEventInfo unnamedParameter2)
    {
        PreventDefaultAction();
    }
}