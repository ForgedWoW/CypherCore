// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

//184783
[SpellScript(184783)]
public class SpellWarrTactician : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleEffectProc(AuraEffect unnamedParameter, ProcEventInfo procInfo)
    {
        PreventDefaultAction();
        var rageSpent = 0;

        var caster = Caster;

        if (caster != null)
            if (procInfo.SpellInfo != null)
            {
                foreach (var cost in procInfo.SpellInfo.CalcPowerCost(caster, procInfo.SpellInfo.GetSchoolMask()))
                {
                    if (cost.Power != PowerType.Rage)
                        continue;

                    rageSpent = cost.Amount;
                }

                if (RandomHelper.randChance((rageSpent / 10) * 1.40))
                {
                    caster.SpellHistory.ResetCooldown(WarriorSpells.COLOSSUS_SMASH, true);
                    caster.SpellHistory.ResetCooldown(WarriorSpells.MORTAL_STRIKE, true);
                    caster.SpellFactory.CastSpell(caster, WarriorSpells.TACTICIAN_CD, true);
                }
            }
    }
}