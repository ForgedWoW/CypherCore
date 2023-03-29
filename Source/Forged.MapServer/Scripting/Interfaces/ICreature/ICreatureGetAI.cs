// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;

namespace Forged.MapServer.Scripting.Interfaces.ICreature;

public interface ICreatureGetAI : IScriptObject
{
    CreatureAI GetAI(Creature creature);
}