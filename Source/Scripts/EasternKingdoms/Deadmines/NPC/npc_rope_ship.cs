// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(49552)]
public class NPCRopeShip : ScriptedAI
{
    public NPCRopeShip(Creature creature) : base(creature) { }

    public override void Reset()
    {
        if (Me.IsSummon)
        {
            var summoner = Me.ToTempSummon().GetSummoner();

            if (summoner != null)
                if (summoner)
                    Me.SpellFactory.CastSpell(summoner, 43785, true);
        }
    }
}