// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        // Dreadstalker - 98035
        [CreatureScript(98035)]
        public class NPCWarlockDreadstalker : PetAI
        {
            public bool FirstTick = true;

            public NPCWarlockDreadstalker(Creature creature) : base(creature)
            {
                if (!Me.TryGetOwner(out Player owner))
                    return;

                creature.SetLevel(owner.Level);
                creature.UpdateLevelDependantStats();
                creature.ReactState = ReactStates.Aggressive;
                creature.SetCreatorGUID(owner.GUID);

                var summon = creature.ToTempSummon();

                if (summon != null)
                {
                    summon.SetCanFollowOwner(true);
                    StartAttackOnOwnersInCombatWith();
                }
            }

            public override void UpdateAI(uint unnamedParameter)
            {
                if (FirstTick)
                {
                    var owner = Me.OwnerUnit;

                    if (!Me.OwnerUnit ||
                        !Me.OwnerUnit.AsPlayer)
                        return;

                    var target = owner.AsPlayer.SelectedUnit;

                    if (target)
                        Me.SpellFactory.CastSpell(target, WarlockSpells.DREADSTALKER_CHARGE, true);

                    FirstTick = false;
                }

                base.UpdateAI(unnamedParameter);
            }
        }
    }
}