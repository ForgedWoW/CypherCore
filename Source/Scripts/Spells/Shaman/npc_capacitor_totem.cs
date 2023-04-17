// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;

namespace Scripts.Spells.Shaman;

//61245
[CreatureScript(61245)]
public class NPCCapacitorTotem : ScriptedAI
{
    public NPCCapacitorTotem(Creature creature) : base(creature) { }

    public override void Reset() { }
}