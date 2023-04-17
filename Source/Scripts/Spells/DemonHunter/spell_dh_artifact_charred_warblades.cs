// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(213010)]
public class SpellDhArtifactCharredWarblades : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var caster = Caster;

        if (caster == null || eventInfo.DamageInfo != null)
            return;

        if (eventInfo.DamageInfo != null || (eventInfo.DamageInfo.SchoolMask & SpellSchoolMask.Fire) == 0)
            return;

        var heal = MathFunctions.CalculatePct(eventInfo.DamageInfo.Damage, aurEff.Amount);
        caster.SpellFactory.CastSpell(caster, ShatteredSoulsSpells.CHARRED_WARBLADES_HEAL, (int)heal);
    }
}