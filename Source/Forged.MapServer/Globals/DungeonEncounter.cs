// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.D;
using Framework.Constants;

namespace Forged.MapServer.Globals;

public class DungeonEncounter
{
    public uint CreditEntry;
    public EncounterCreditType CreditType;
    public DungeonEncounterRecord DBCEntry;
    public uint LastEncounterDungeon;

    public DungeonEncounter(DungeonEncounterRecord dbcEntry, EncounterCreditType creditType, uint creditEntry, uint lastEncounterDungeon)
    {
        DBCEntry = dbcEntry;
        CreditType = creditType;
        CreditEntry = creditEntry;
        LastEncounterDungeon = lastEncounterDungeon;
    }
}