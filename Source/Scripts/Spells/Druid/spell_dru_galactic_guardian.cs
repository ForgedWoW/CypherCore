// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 203964 - Galactic Guardian
internal class SpellDruGalacticGuardian : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var damageInfo = eventInfo.DamageInfo;

        if (damageInfo != null)
        {
            var target = Target;

            // free automatic moonfire on Target
            target.SpellFactory.CastSpell(damageInfo.Victim, DruidSpellIds.MoonfireDamage, true);

            // Cast aura
            target.SpellFactory.CastSpell(damageInfo.Victim, DruidSpellIds.GalacticGuardianAura, true);
        }
    }
}