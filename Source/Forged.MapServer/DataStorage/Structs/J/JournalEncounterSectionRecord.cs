using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.J;

public sealed record JournalEncounterSectionRecord
{
    public uint Id;
    public LocalizedString Title;
    public LocalizedString BodyText;
    public ushort JournalEncounterID;
    public byte OrderIndex;
    public ushort ParentSectionID;
    public ushort FirstChildSectionID;
    public ushort NextSiblingSectionID;
    public byte Type;
    public uint IconCreatureDisplayInfoID;
    public int UiModelSceneID;
    public int SpellID;
    public int IconFileDataID;
    public int Flags;
    public int IconFlags;
    public sbyte DifficultyMask;
}