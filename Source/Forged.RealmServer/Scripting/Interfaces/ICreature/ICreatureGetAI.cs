// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.AI;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Creatures;

namespace Forged.RealmServer.Scripting.Interfaces.ICreature;

public interface ICreatureGetAI : IScriptObject
{
	CreatureAI GetAI(Creature creature);
}