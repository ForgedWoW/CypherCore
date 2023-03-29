﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 49028 - Dancing Rune Weapon
internal class spell_dk_dancing_rune_weapon : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    // This is a port of the old switch hack in Unit.cpp, it's not correct
    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var caster = Caster;

        if (!caster)
            return;

        Unit drw = null;

        foreach (var controlled in caster.Controlled)
            if (controlled.Entry == CreatureIds.DancingRuneWeapon)
            {
                drw = controlled;

                break;
            }

        if (!drw ||
            !drw.Victim)
            return;

        var spellInfo = eventInfo.SpellInfo;

        if (spellInfo == null)
            return;

        var damageInfo = eventInfo.DamageInfo;

        if (damageInfo == null ||
            damageInfo.Damage == 0)
            return;

        var amount = (int)damageInfo.Damage / 2;
        SpellNonMeleeDamage log = new(drw, drw.Victim, spellInfo, new SpellCastVisual(spellInfo.GetSpellXSpellVisualId(drw), 0), spellInfo.GetSchoolMask());
        log.Damage = (uint)amount;
        Unit.DealDamage(drw, drw.Victim, (uint)amount, null, DamageEffectType.SpellDirect, spellInfo.GetSchoolMask(), spellInfo, true);
        drw.SendSpellNonMeleeDamageLog(log);
    }
}