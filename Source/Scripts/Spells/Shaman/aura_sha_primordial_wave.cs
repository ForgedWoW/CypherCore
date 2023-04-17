// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

[SpellScript(new uint[]
{
    285452, 188196, 77472
})]
public class AuraShaPrimordialWave : SpellScript, ISpellAfterCast, ISpellCalculateMultiplier
{
    public void AfterCast()
    {
        var player = Caster.AsPlayer;

        if (player == null)
            return;

        if (Spell.IsTriggered && !player.HasAura(ShamanSpells.LAVA_SURGE_CAST_TIME))
            return;

        if (!player.HasAura(ShamanSpells.PRIMORDIAL_WAVE_AURA))
            return;

        var spec = player.GetPrimarySpecialization();
        var spellId = SpellInfo.Id;

        uint procSpell = 0;
        List<Unit> targets = new();

        if (spec == TalentSpecialization.ShamanElemental && spellId == ShamanSpells.LavaBurst)
        {
            player.GetEnemiesWithinRangeWithOwnedAura(targets, 100.0f, ShamanSpells.FlameShock);
            procSpell = ShamanSpells.LavaBurst;
        }
        else if (spec == TalentSpecialization.ShamanEnhancement && spellId == ShamanSpells.LIGHTNING_BOLT)
        {
            player.GetEnemiesWithinRangeWithOwnedAura(targets, 100.0f, ShamanSpells.FlameShock);
            procSpell = ShamanSpells.LIGHTNING_BOLT;
        }
        else if (spec == TalentSpecialization.ShamanRestoration && spellId == ShamanSpells.HEALING_WAVE)
        {
            player.GetAlliesWithinRangeWithOwnedAura(targets, 100.0f, ShamanSpells.RIPTIDE);
            procSpell = ShamanSpells.RIPTIDE;
        }

        if (procSpell != 0)
        {
            foreach (var target in targets)
                player.SpellFactory.CastSpell(target, procSpell, true);

            player.RemoveAura(ShamanSpells.PRIMORDIAL_WAVE_AURA);
        }
    }

    public double CalcMultiplier(double multiplier)
    {
        var player = Caster.AsPlayer;

        if (player == null || !player.HasAura(ShamanSpells.PRIMORDIAL_WAVE_AURA))
            return multiplier;

        var spec = player.GetPrimarySpecialization();
        var spellId = SpellInfo.Id;

        if (spec == TalentSpecialization.ShamanElemental && spellId == ShamanSpells.LavaBurst)
        {
            var primordialWave = SpellManager.Instance.GetSpellInfo(ShamanSpells.PRIMORDIAL_WAVE);
            var pct = primordialWave.GetEffect(2).BasePoints * 0.01f;
            multiplier *= 1f + pct;
        }
        else if (spec == TalentSpecialization.ShamanEnhancement && spellId == ShamanSpells.LIGHTNING_BOLT)
        {
            var primordialWave = SpellManager.Instance.GetSpellInfo(ShamanSpells.PRIMORDIAL_WAVE);
            var pct = primordialWave.GetEffect(3).BasePoints * 0.01f;
            multiplier *= 1f + pct;
        }
        else if (spec == TalentSpecialization.ShamanRestoration && spellId == ShamanSpells.HEALING_WAVE)
        {
            var primordialWave = SpellManager.Instance.GetSpellInfo(ShamanSpells.PRIMORDIAL_WAVE);
            var pct = primordialWave.GetEffect(1).BasePoints * 0.01f;
            multiplier *= 1f + pct;
        }

        return multiplier;
    }
}