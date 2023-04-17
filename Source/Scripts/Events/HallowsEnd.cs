// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.m_Events.HallowsEnd;

internal struct SpellIds
{
    //HallowEndCandysSpells
    public const uint CANDY_ORANGE_GIANT = 24924;        // Effect 1: Apply Aura: Mod Size, Value: 30%
    public const uint CANDY_SKELETON = 24925;           // Effect 1: Apply Aura: Change Model (Skeleton). Effect 2: Apply Aura: Underwater Breathing
    public const uint CANDY_PIRATE = 24926;             // Effect 1: Apply Aura: Increase Swim Speed, Value: 50%
    public const uint CANDY_GHOST = 24927;              // Effect 1: Apply Aura: Levitate / Hover. Effect 2: Apply Aura: Slow Fall, Effect 3: Apply Aura: Water Walking
    public const uint CANDY_FEMALE_DEFIAS_PIRATE = 44742; // Effect 1: Apply Aura: Change Model (Defias Pirate, Female). Effect 2: Increase Swim Speed, Value: 50%
    public const uint CANDY_MALE_DEFIAS_PIRATE = 44743;   // Effect 1: Apply Aura: Change Model (Defias Pirate, Male).   Effect 2: Increase Swim Speed, Value: 50%

    //Trickspells
    public const uint PIRATE_COSTUME_MALE = 24708;
    public const uint PIRATE_COSTUME_FEMALE = 24709;
    public const uint NINJA_COSTUME_MALE = 24710;
    public const uint NINJA_COSTUME_FEMALE = 24711;
    public const uint LEPER_GNOME_COSTUME_MALE = 24712;
    public const uint LEPER_GNOME_COSTUME_FEMALE = 24713;
    public const uint SKELETON_COSTUME = 24723;
    public const uint GHOST_COSTUME_MALE = 24735;
    public const uint GHOST_COSTUME_FEMALE = 24736;
    public const uint TRICK_BUFF = 24753;

    //Trickortreatspells
    public const uint TRICK = 24714;
    public const uint TREAT = 24715;
    public const uint TRICKED_OR_TREATED = 24755;
    public const uint TRICKY_TREAT_SPEED = 42919;
    public const uint TRICKY_TREAT_TRIGGER = 42965;
    public const uint UPSET_TUMMY = 42966;

    //Wand Spells
    public const uint HALLOWED_WAND_PIRATE = 24717;
    public const uint HALLOWED_WAND_NINJA = 24718;
    public const uint HALLOWED_WAND_LEPER_GNOME = 24719;
    public const uint HALLOWED_WAND_RANDOM = 24720;
    public const uint HALLOWED_WAND_SKELETON = 24724;
    public const uint HALLOWED_WAND_WISP = 24733;
    public const uint HALLOWED_WAND_GHOST = 24737;
    public const uint HALLOWED_WAND_BAT = 24741;
}

[Script] // 24930 - Hallow's End Candy
internal class SpellHallowEndCandySpellScript : SpellScript, IHasSpellEffects
{
    private readonly uint[] _spells =
    {
        SpellIds.CANDY_ORANGE_GIANT, SpellIds.CANDY_SKELETON, SpellIds.CANDY_PIRATE, SpellIds.CANDY_GHOST
    };

    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.Hit));
    }

    private void HandleDummy(int effIndex)
    {
        Caster.SpellFactory.CastSpell(Caster, _spells.SelectRandom(), true);
    }
}

[Script] // 24926 - Hallow's End Candy
internal class SpellHallowEndCandyPirateAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.ModIncreaseSwimSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.ModIncreaseSwimSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var spell = Target.NativeGender == Gender.Female ? SpellIds.CANDY_FEMALE_DEFIAS_PIRATE : SpellIds.CANDY_MALE_DEFIAS_PIRATE;
        Target.SpellFactory.CastSpell(Target, spell, true);
    }

    private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var spell = Target.NativeGender == Gender.Female ? SpellIds.CANDY_FEMALE_DEFIAS_PIRATE : SpellIds.CANDY_MALE_DEFIAS_PIRATE;
        Target.RemoveAura(spell);
    }
}

[Script] // 24750 Trick
internal class SpellHallowEndTrick : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster;
        var target = HitPlayer;

        if (target)
        {
            var gender = target.NativeGender;
            var spellId = SpellIds.TRICK_BUFF;

            switch (RandomHelper.URand(0, 5))
            {
                case 1:
                    spellId = gender == Gender.Female ? SpellIds.LEPER_GNOME_COSTUME_FEMALE : SpellIds.LEPER_GNOME_COSTUME_MALE;

                    break;
                case 2:
                    spellId = gender == Gender.Female ? SpellIds.PIRATE_COSTUME_FEMALE : SpellIds.PIRATE_COSTUME_MALE;

                    break;
                case 3:
                    spellId = gender == Gender.Female ? SpellIds.GHOST_COSTUME_FEMALE : SpellIds.GHOST_COSTUME_MALE;

                    break;
                case 4:
                    spellId = gender == Gender.Female ? SpellIds.NINJA_COSTUME_FEMALE : SpellIds.NINJA_COSTUME_MALE;

                    break;
                case 5:
                    spellId = SpellIds.SKELETON_COSTUME;

                    break;
            }

            caster.SpellFactory.CastSpell(target, spellId, true);
        }
    }
}

[Script] // 24751 Trick or Treat
internal class SpellHallowEndTrickOrTreat : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster;
        var target = HitPlayer;

        if (target)
        {
            caster.SpellFactory.CastSpell(target, RandomHelper.randChance(50) ? SpellIds.TRICK : SpellIds.TREAT, true);
            caster.SpellFactory.CastSpell(target, SpellIds.TRICKED_OR_TREATED, true);
        }
    }
}

[Script] // 44436 - Tricky Treat
internal class SpellHallowEndTrickyTreat : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster;

        if (caster.HasAura(SpellIds.TRICKY_TREAT_TRIGGER) &&
            caster.GetAuraCount(SpellIds.TRICKY_TREAT_SPEED) > 3 &&
            RandomHelper.randChance(33))
            caster.SpellFactory.CastSpell(caster, SpellIds.UPSET_TUMMY, true);
    }
}

[Script] // 24717, 24718, 24719, 24720, 24724, 24733, 24737, 24741
internal class SpellHallowEndWand : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;
        var target = HitUnit;

        uint spellId;
        var female = target.NativeGender == Gender.Female;

        switch (SpellInfo.Id)
        {
            case SpellIds.HALLOWED_WAND_LEPER_GNOME:
                spellId = female ? SpellIds.LEPER_GNOME_COSTUME_FEMALE : SpellIds.LEPER_GNOME_COSTUME_MALE;

                break;
            case SpellIds.HALLOWED_WAND_PIRATE:
                spellId = female ? SpellIds.PIRATE_COSTUME_FEMALE : SpellIds.PIRATE_COSTUME_MALE;

                break;
            case SpellIds.HALLOWED_WAND_GHOST:
                spellId = female ? SpellIds.GHOST_COSTUME_FEMALE : SpellIds.GHOST_COSTUME_MALE;

                break;
            case SpellIds.HALLOWED_WAND_NINJA:
                spellId = female ? SpellIds.NINJA_COSTUME_FEMALE : SpellIds.NINJA_COSTUME_MALE;

                break;
            case SpellIds.HALLOWED_WAND_RANDOM:
                spellId = RandomHelper.RAND(SpellIds.HALLOWED_WAND_PIRATE, SpellIds.HALLOWED_WAND_NINJA, SpellIds.HALLOWED_WAND_LEPER_GNOME, SpellIds.HALLOWED_WAND_SKELETON, SpellIds.HALLOWED_WAND_WISP, SpellIds.HALLOWED_WAND_GHOST, SpellIds.HALLOWED_WAND_BAT);

                break;
            default:
                return;
        }

        caster.SpellFactory.CastSpell(target, spellId, true);
    }
}