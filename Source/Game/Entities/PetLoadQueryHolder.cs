// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Database;

namespace Game.Entities;

class PetLoadQueryHolder : SQLQueryHolder<PetLoginQueryLoad>
{
	public PetLoadQueryHolder(ulong ownerGuid, uint petNumber)
	{
		var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_DECLINED_NAME);
		stmt.AddValue(0, ownerGuid);
		stmt.AddValue(1, petNumber);
		SetQuery(PetLoginQueryLoad.DeclinedNames, stmt);

		stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_AURA);
		stmt.AddValue(0, petNumber);
		SetQuery(PetLoginQueryLoad.Auras, stmt);

		stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_AURA_EFFECT);
		stmt.AddValue(0, petNumber);
		SetQuery(PetLoginQueryLoad.AuraEffects, stmt);

		stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_SPELL);
		stmt.AddValue(0, petNumber);
		SetQuery(PetLoginQueryLoad.Spells, stmt);

		stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_SPELL_COOLDOWN);
		stmt.AddValue(0, petNumber);
		SetQuery(PetLoginQueryLoad.Cooldowns, stmt);

		stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_SPELL_CHARGES);
		stmt.AddValue(0, petNumber);
		SetQuery(PetLoginQueryLoad.Charges, stmt);
	}
}