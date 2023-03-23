// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.AI;
using Game.Entities;

namespace Forged.RealmServer.Scripting.Interfaces.IGameObject;

public interface IGameObjectGetAI : IScriptObject
{
	GameObjectAI GetAI(GameObject go);
}