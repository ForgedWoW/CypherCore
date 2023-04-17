// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.m_Events.WinterVeil;

internal struct SpellIds
{
    //Mistletoe
    public const uint CREATE_MISTLETOE = 26206;
    public const uint CREATE_HOLLY = 26207;
    public const uint CREATE_SNOWFLAKES = 45036;

    //Winter Wondervolt
    public const uint PX238_WINTER_WONDERVOLT_TRANSFORM1 = 26157;
    public const uint PX238_WINTER_WONDERVOLT_TRANSFORM2 = 26272;
    public const uint PX238_WINTER_WONDERVOLT_TRANSFORM3 = 26273;
    public const uint PX238_WINTER_WONDERVOLT_TRANSFORM4 = 26274;

    //Reindeertransformation
    public const uint FLYING_REINDEER310 = 44827;
    public const uint FLYING_REINDEER280 = 44825;
    public const uint FLYING_REINDEER60 = 44824;
    public const uint REINDEER100 = 25859;
    public const uint REINDEER60 = 25858;
}

[Script] // 26218 - Mistletoe
internal class SpellWinterVeilMistletoe : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var target = HitPlayer;

        if (target)
        {
            var spellId = RandomHelper.RAND(SpellIds.CREATE_HOLLY, SpellIds.CREATE_MISTLETOE, SpellIds.CREATE_SNOWFLAKES);
            Caster.SpellFactory.CastSpell(target, spellId, true);
        }
    }
}

[Script] // 26275 - PX-238 Winter Wondervolt TRAP
internal class SpellWinterVeilPx238WinterWondervolt : SpellScript, IHasSpellEffects
{
    private static readonly uint[] Spells =
    {
        SpellIds.PX238_WINTER_WONDERVOLT_TRANSFORM1, SpellIds.PX238_WINTER_WONDERVOLT_TRANSFORM2, SpellIds.PX238_WINTER_WONDERVOLT_TRANSFORM3, SpellIds.PX238_WINTER_WONDERVOLT_TRANSFORM4
    };

    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        var target = HitUnit;

        if (target)
        {
            for (byte i = 0; i < 4; ++i)
                if (target.HasAura(Spells[i]))
                    return;

            target.SpellFactory.CastSpell(target, Spells[RandomHelper.URand(0, 3)], true);
        }
    }
}

[Script]
internal class SpellItemReindeerTransformation : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;

        if (caster.HasAuraType(AuraType.Mounted))
        {
            double flyspeed = caster.GetSpeedRate(UnitMoveType.Flight);
            double speed = caster.GetSpeedRate(UnitMoveType.Run);

            caster.RemoveAurasByType(AuraType.Mounted);
            //5 different spells used depending on mounted speed and if Mount can fly or not

            if (flyspeed >= 4.1f)
                // Flying Reindeer
                caster.SpellFactory.CastSpell(caster, SpellIds.FLYING_REINDEER310, true); //310% flying Reindeer
            else if (flyspeed >= 3.8f)
                // Flying Reindeer
                caster.SpellFactory.CastSpell(caster, SpellIds.FLYING_REINDEER280, true); //280% flying Reindeer
            else if (flyspeed >= 1.6f)
                // Flying Reindeer
                caster.SpellFactory.CastSpell(caster, SpellIds.FLYING_REINDEER60, true); //60% flying Reindeer
            else if (speed >= 2.0f)
                // Reindeer
                caster.SpellFactory.CastSpell(caster, SpellIds.REINDEER100, true); //100% ground Reindeer
            else
                // Reindeer
                caster.SpellFactory.CastSpell(caster, SpellIds.REINDEER60, true); //60% ground Reindeer
        }
    }
}