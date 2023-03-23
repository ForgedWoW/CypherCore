// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Database;
using Game.Entities;
using Game.Common.Entities;

namespace Game.Common.Entities;

class PetLoadQueryHolder : SQLQueryHolder<PetLoginQueryLoad>
{
	public PetLoadQueryHolder(ulong ownerGuid, uint petNumber)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_DECLINED_NAME);
		stmt.AddValue(0, ownerGuid);
		stmt.AddValue(1, petNumber);
		SetQuery(PetLoginQueryLoad.DeclinedNames, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_AURA);
		stmt.AddValue(0, petNumber);
		SetQuery(PetLoginQueryLoad.Auras, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_AURA_EFFECT);
		stmt.AddValue(0, petNumber);
		SetQuery(PetLoginQueryLoad.AuraEffects, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_SPELL);
		stmt.AddValue(0, petNumber);
		SetQuery(PetLoginQueryLoad.Spells, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_SPELL_COOLDOWN);
		stmt.AddValue(0, petNumber);
		SetQuery(PetLoginQueryLoad.Cooldowns, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_SPELL_CHARGES);
		stmt.AddValue(0, petNumber);
		SetQuery(PetLoginQueryLoad.Charges, stmt);
	}
}
