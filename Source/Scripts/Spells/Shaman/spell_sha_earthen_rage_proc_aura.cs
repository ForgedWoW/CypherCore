// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 170377 - Earthen Rage (Proc Aura)
[SpellScript(170377)]
internal class SpellShaEarthenRageProcAura : AuraScript, IHasAuraEffects
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
            var earthenRageScript = aura.GetScript<SpellShaEarthenRagePassive>();

            if (earthenRageScript != null)
            {
                var procTarget = Global.ObjAccessor.GetUnit(Caster, earthenRageScript.GetProcTargetGuid());

                if (procTarget)
                    Target.SpellFactory.CastSpell(procTarget, ShamanSpells.EarthenRageDamage, true);
            }
        }
    }
}