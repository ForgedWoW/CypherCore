// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Database;

namespace Forged.MapServer.Entities;

internal class PetLoadQueryHolder : SQLQueryHolder<PetLoginQueryLoad>
{
    public PetLoadQueryHolder(ulong ownerGuid, uint petNumber, CharacterDatabase characterDatabase)
    {
        var stmt = characterDatabase.GetPreparedStatement(CharStatements.SEL_PET_DECLINED_NAME);
        stmt.AddValue(0, ownerGuid);
        stmt.AddValue(1, petNumber);
        SetQuery(PetLoginQueryLoad.DeclinedNames, stmt);

        stmt = characterDatabase.GetPreparedStatement(CharStatements.SEL_PET_AURA);
        stmt.AddValue(0, petNumber);
        SetQuery(PetLoginQueryLoad.Auras, stmt);

        stmt = characterDatabase.GetPreparedStatement(CharStatements.SEL_PET_AURA_EFFECT);
        stmt.AddValue(0, petNumber);
        SetQuery(PetLoginQueryLoad.AuraEffects, stmt);

        stmt = characterDatabase.GetPreparedStatement(CharStatements.SEL_PET_SPELL);
        stmt.AddValue(0, petNumber);
        SetQuery(PetLoginQueryLoad.Spells, stmt);

        stmt = characterDatabase.GetPreparedStatement(CharStatements.SEL_PET_SPELL_COOLDOWN);
        stmt.AddValue(0, petNumber);
        SetQuery(PetLoginQueryLoad.Cooldowns, stmt);

        stmt = characterDatabase.GetPreparedStatement(CharStatements.SEL_PET_SPELL_CHARGES);
        stmt.AddValue(0, petNumber);
        SetQuery(PetLoginQueryLoad.Charges, stmt);
    }
}