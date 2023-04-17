// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 329910 - Eclipse out of combat - ECLIPSE_OOC
internal class SpellDruEclipseOoc : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(Tick, 0, AuraType.PeriodicDummy));
    }

    private void Tick(AuraEffect aurEff)
    {
        var owner = Target;
        var auraEffDummy = owner.GetAuraEffect(DruidSpellIds.EclipseDummy, 0);

        if (auraEffDummy == null)
            return;

        if (!owner.IsInCombat &&
            (!owner.HasAura(DruidSpellIds.EclipseSolarSpellCnt) || !owner.HasAura(DruidSpellIds.EclipseLunarSpellCnt)))
        {
            // Restore 2 stacks to each spell when out of combat
            SpellDruEclipseCommon.SetSpellCount(owner, DruidSpellIds.EclipseSolarSpellCnt, (uint)auraEffDummy.Amount);
            SpellDruEclipseCommon.SetSpellCount(owner, DruidSpellIds.EclipseLunarSpellCnt, (uint)auraEffDummy.Amount);
        }
    }
}