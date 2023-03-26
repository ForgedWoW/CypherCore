using Forged.MapServer.DataStorage.Structs.D;
using Framework.Constants;

namespace Forged.MapServer.Globals;

public class DungeonEncounter
{
    public DungeonEncounterRecord DBCEntry;
    public EncounterCreditType CreditType;
    public uint CreditEntry;
    public uint LastEncounterDungeon;

    public DungeonEncounter(DungeonEncounterRecord dbcEntry, EncounterCreditType creditType, uint creditEntry, uint lastEncounterDungeon)
    {
        DBCEntry = dbcEntry;
        CreditType = creditType;
        CreditEntry = creditEntry;
        LastEncounterDungeon = lastEncounterDungeon;
    }
}