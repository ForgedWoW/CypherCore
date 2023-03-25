// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.BattleFields;
using Game.Common.Maps;

namespace Forged.RealmServer.Scripting.Interfaces.IBattlefield;

public interface IBattlefieldGetBattlefield : IScriptObject
{
	BattleField GetBattlefield(Map map);
}