// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.STASIS)]
internal class aura_evoker_stasis : AuraScript, IAuraOnProc, IAuraOnApply, IAuraOverrideProcInfo
{
    List<ObjectGuid> _seenSpells = new();

    public SpellProcEntry SpellProcEntry { get; } = new();

    public void AuraApply()
    {
        Aura.SetStackAmount(3);

        SpellProcEntry.Chance = 100;
        SpellProcEntry.ProcFlags = new ProcFlagsInit();
        SpellProcEntry.ProcFlags.Or(ProcFlags.DealHelpfulSpell | ProcFlags.DealHelpfulAbility | ProcFlags.DealHelpfulPeriodic);
        SpellProcEntry.HitMask = ProcFlagsHit.None;
        SpellProcEntry.ProcsPerMinute = 0;
        SpellProcEntry.Charges = 0;
        SpellProcEntry.Cooldown = 0;
        SpellProcEntry.DisableEffectsMask = 0;
        SpellProcEntry.SchoolMask = SpellSchoolMask.None;
        SpellProcEntry.SpellFamilyMask = new(2, 538968064, 0, 0);
        SpellProcEntry.SpellFamilyName = SpellFamilyNames.Evoker;
        SpellProcEntry.SpellTypeMask = ProcFlagsSpellType.Heal;
        SpellProcEntry.SpellPhaseMask = ProcFlagsSpellPhase.Cast;
    }

    public void OnProc(ProcEventInfo info)
    {
        if (!Caster.TryGetAsPlayer(out var player) || _seenSpells.Contains(info.ProcSpell.CastId))
            return;

        _seenSpells.Add(info.ProcSpell.CastId);

        if (!player.TryGetAura(EvokerSpells.STASIS_ORB_AURA_1, out var orbAura))
            orbAura = player.AddAura(EvokerSpells.STASIS_ORB_AURA_1);
        else if (!player.TryGetAura(EvokerSpells.STASIS_ORB_AURA_2, out orbAura))
            orbAura = player.AddAura(EvokerSpells.STASIS_ORB_AURA_2);
        else if (!player.TryGetAura(EvokerSpells.STASIS_ORB_AURA_3, out orbAura))
            orbAura = player.AddAura(EvokerSpells.STASIS_ORB_AURA_3);

        orbAura.ForEachAuraScript<IAuraScriptValues>(a => a.ScriptValues["spell"] = info.ProcSpell);

        Aura.ModStackAmount(-1);
    }
}