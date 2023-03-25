// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Scripting.Interfaces.IFormula;

public interface IFormulaOnGainCalculation : IScriptObject
{
	void OnGainCalculation(uint gain, Player player, Unit unit);
}