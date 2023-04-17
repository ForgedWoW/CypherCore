// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// Summon Fire, Earth & Storm Elemental  - Called By 198067 Fire Elemental, 198103 Earth Elemental, 192249 Storm Elemental
[SpellScript(new uint[]
{
    198067, 198103, 192249
})]
public class SpellShamanGenericSummonElemental : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleSummon, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleSummon(int effIndex)
    {
        uint triggerSpell;

        switch (SpellInfo.Id)
        {
            case Spells.SUMMON_FIRE_ELEMENTAL:
                triggerSpell = (Caster.HasAura(Spells.PRIMAL_ELEMENTALIST)) ? Spells.SUMMON_PRIMAL_ELEMENTALIST_FIRE_ELEMENTAL : Spells.SUMMON_FIRE_ELEMENTAL_TRIGGERED;

                break;
            case Spells.SUMMON_EARTH_ELEMENTAL:
                triggerSpell = (Caster.HasAura(Spells.PRIMAL_ELEMENTALIST)) ? Spells.SUMMON_PRIMAL_ELEMENTALIST_EARTH_ELEMENTAL : Spells.SUMMON_EARTH_ELEMENTAL_TRIGGERED;

                break;
            case Spells.SUMMON_STORM_ELEMENTAL:
                triggerSpell = (Caster.HasAura(Spells.PRIMAL_ELEMENTALIST)) ? Spells.SUMMON_PRIMAL_ELEMENTALIST_STORM_ELEMENTAL : Spells.SUMMON_STORM_ELEMENTAL_TRIGGERED;

                break;
            default:
                triggerSpell = 0;

                break;
        }

        if (triggerSpell != 0)
            Caster.SpellFactory.CastSpell(Caster, triggerSpell, true);
    }

    private struct Spells
    {
        public const uint PRIMAL_ELEMENTALIST = 117013;
        public const uint SUMMON_FIRE_ELEMENTAL = 198067;
        public const uint SUMMON_FIRE_ELEMENTAL_TRIGGERED = 188592;
        public const uint SUMMON_PRIMAL_ELEMENTALIST_FIRE_ELEMENTAL = 118291;
        public const uint SUMMON_EARTH_ELEMENTAL = 198103;
        public const uint SUMMON_EARTH_ELEMENTAL_TRIGGERED = 188616;
        public const uint SUMMON_PRIMAL_ELEMENTALIST_EARTH_ELEMENTAL = 118323;
        public const uint SUMMON_STORM_ELEMENTAL = 192249;
        public const uint SUMMON_STORM_ELEMENTAL_TRIGGERED = 157299;
        public const uint SUMMON_PRIMAL_ELEMENTALIST_STORM_ELEMENTAL = 157319;
    }
}