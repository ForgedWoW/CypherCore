// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[SpellScript(34026)]
public class SpellHunKillCommand : SpellScript, IHasSpellEffects, ISpellCheckCast
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public SpellCastResult CheckCast()
    {
        Unit pet = Caster.GetGuardianPet();
        var petTarget = ExplTargetUnit;

        if (pet == null || pet.IsDead)
            return SpellCastResult.NoPet;

        // pet has a target and target is within 5 yards and target is in line of sight
        if (petTarget == null || !pet.IsWithinDist(petTarget, 40.0f, true) || !petTarget.IsWithinLOSInMap(pet))
            return SpellCastResult.DontReport;

        if (pet.HasAuraType(AuraType.ModStun) || pet.HasAuraType(AuraType.ModConfuse) || pet.HasAuraType(AuraType.ModSilence) || pet.HasAuraType(AuraType.ModFear) || pet.HasAuraType(AuraType.ModFear2))
            return SpellCastResult.CantDoThatRightNow;

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        if (Caster.IsPlayer)
        {
            Unit pet = Caster.GetGuardianPet();

            if (pet != null)
            {
                if (!pet)
                    return;

                if (!ExplTargetUnit)
                    return;

                var target = ExplTargetUnit;
                var player = Caster.AsPlayer;

                pet.SpellFactory.CastSpell(ExplTargetUnit, HunterSpells.KILL_COMMAND_TRIGGER, true);

                if (pet.Victim)
                {
                    pet.AttackStop();
                    pet.AsCreature.AI.AttackStart(ExplTargetUnit);
                }
                else
                {
                    pet.AsCreature.AI.AttackStart(ExplTargetUnit);
                }
                //pet->CastSpell(GetExplTargetUnit(), KILL_COMMAND_CHARGE, true);

                //191384 Aspect of the Beast
                if (Caster.HasAura(Sspell.ASPECTOFTHE_BEAST))
                {
                    if (pet.HasAura(Sspell.SPIKED_COLLAR))
                        player.SpellFactory.CastSpell(target, Sspell.BESTIAL_FEROCITY, true);

                    if (pet.HasAura(Sspell.GREAT_STAMINA))
                        pet.SpellFactory.CastSpell(pet, Sspell.BESTIAL_TENACITY, true);

                    if (pet.HasAura(Sspell.CORNERED))
                        player.SpellFactory.CastSpell(target, Sspell.BESTIAL_CUNNING, true);
                }
            }
        }
    }

    private struct Sspell
    {
        public const uint ANIMAL_INSTINCTS_REDUCTION = 232646;
        public const uint ASPECTOFTHE_BEAST = 191384;
        public const uint BESTIAL_FEROCITY = 191413;
        public const uint BESTIAL_TENACITY = 191414;
        public const uint BESTIAL_CUNNING = 191397;
        public const uint SPIKED_COLLAR = 53184;
        public const uint GREAT_STAMINA = 61688;
        public const uint CORNERED = 53497;
    }
}