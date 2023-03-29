// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Scripting.Interfaces.ISpell;

public interface ISpellCalcCritChance : ISpellScript
{
    void CalcCritChance(Unit victim, ref double chance);
}