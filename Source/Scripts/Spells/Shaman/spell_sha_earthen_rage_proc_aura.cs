﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 170377 - Earthen Rage (Proc Aura)
[SpellScript(170377)]
internal class spell_sha_earthen_rage_proc_aura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }

    private void HandleEffectPeriodic(AuraEffect aurEff)
    {
        PreventDefaultAction();
        var aura = Caster.GetAura(ShamanSpells.EarthenRagePassive);

        if (aura != null)
        {
            var earthen_rage_script = aura.GetScript<spell_sha_earthen_rage_passive>();

            if (earthen_rage_script != null)
            {
                var procTarget = Global.ObjAccessor.GetUnit(Caster, earthen_rage_script.GetProcTargetGuid());

                if (procTarget)
                    Target.CastSpell(procTarget, ShamanSpells.EarthenRageDamage, true);
            }
        }
    }
}