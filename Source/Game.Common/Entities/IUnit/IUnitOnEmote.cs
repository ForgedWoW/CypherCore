// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Units;
using Game.Common.Scripting.Interfaces;

namespace Game.Common.Entities.IUnit;

public interface IUnitOnEmote : IScriptObject
{
	void OnEmote(Unit target, int emoteId);
}