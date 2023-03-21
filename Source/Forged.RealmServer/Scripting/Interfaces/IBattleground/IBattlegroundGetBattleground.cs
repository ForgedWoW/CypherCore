// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.BattleGrounds;

namespace Forged.RealmServer.Scripting.Interfaces.IBattleground;

public interface IBattlegroundGetBattleground : IScriptObject
{
	Battleground GetBattleground();
}