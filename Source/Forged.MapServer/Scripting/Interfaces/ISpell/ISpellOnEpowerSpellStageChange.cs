// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.DataStorage.Structs.S;

namespace Game.Scripting.Interfaces.ISpell;

public interface ISpellOnEpowerSpellStageChange : ISpellScript
{
	void EmpowerSpellStageChange(SpellEmpowerStageRecord oldStage, SpellEmpowerStageRecord newStage);
}