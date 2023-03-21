// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.DataStorage;

namespace Forged.RealmServer.Scripting.Interfaces.ISpell;

public interface ISpellOnEpowerSpellStageChange : ISpellScript
{
	void EmpowerSpellStageChange(SpellEmpowerStageRecord oldStage, SpellEmpowerStageRecord newStage);
}