// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.AI;
using Game.Entities;
using Game.Common.Entities.Creatures;

namespace Forged.RealmServer.Scripting.Interfaces.ICreature;

public interface ICreatureGetAI : IScriptObject
{
	CreatureAI GetAI(Creature creature);
}