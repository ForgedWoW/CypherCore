// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Dynamic;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.STASIS)]
internal class AuraEvokerStasis : AuraScript, IAuraOnProc, IAuraOnApply, IAuraOverrideProcInfo
{
    readonly List<ObjectGuid> _seenSpells = new();

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
        SpellProcEntry.SpellFamilyMask = new FlagArray128(2, 538968064, 0, 0);
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