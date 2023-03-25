// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.S;

namespace Forged.MapServer.Scripting.Interfaces.ISpell;

public interface ISpellOnEpowerSpellEnd : ISpellScript
{
	void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta);
}