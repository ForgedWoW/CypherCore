﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DemonHunter;

[SpellScript(213480)]
public class spell_demon_hunter_unending_hatred : AuraScript, IAuraCheckProc, IAuraOnProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.DamageInfo != null && eventInfo.DamageInfo.SchoolMask.HasFlag(SpellSchoolMask.Shadow);
    }

    public void OnProc(ProcEventInfo eventInfo)
    {
        var caster = GetPlayerCaster();

        if (caster == null)
            return;

        var pointsGained = GetPointsGained(caster, eventInfo.DamageInfo.Damage);

        if (caster.GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
            caster.EnergizeBySpell(caster, SpellInfo, pointsGained, PowerType.Fury);
        else if (caster.GetPrimarySpecialization() == TalentSpecialization.DemonHunterVengeance)
            caster.EnergizeBySpell(caster, SpellInfo, pointsGained, PowerType.Pain);
    }

    public Player GetPlayerCaster()
    {
        var caster = Caster;

        if (caster == null)
            return null;

        return caster.AsPlayer;
    }

    public double GetPointsGained(Player caster, double damage)
    {
        var damagePct = damage / caster.MaxHealth * 100.0f / 2;
        var max = SpellInfo.GetEffect(0).BasePoints;

        if (damagePct > max)
            return max;

        if (damagePct < 1F)
            return 1;

        return 0;
    }
}