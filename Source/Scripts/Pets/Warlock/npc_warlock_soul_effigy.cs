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
        // 103679 - Soul Effigy
        [CreatureScript(103679)]
        public class NPCWarlockSoulEffigy : CreatureAI
        {
            public NPCWarlockSoulEffigy(Creature creature) : base(creature)
            {
                if (!Me.TryGetOwner(out Player owner))
                    return;

                creature.SetLevel(owner.Level);
                creature.UpdateLevelDependantStats();
                creature.ReactState = ReactStates.Aggressive;
            }

            public override void Reset()
            {
                Me.SetControlled(true, UnitState.Root);
                Me.SpellFactory.CastSpell(Me, WarlockSpells.SOUL_EFFIGY_AURA, true);
            }

            public override void UpdateAI(uint unnamedParameter) { }
        }
    }
}