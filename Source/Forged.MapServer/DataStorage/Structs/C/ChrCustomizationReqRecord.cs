using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class ChrCustomizationReqRecord
{
    public uint Id;
    public long RaceMask;
    public string ReqSource;
    public int Flags;
    public int ClassMask;
    public int AchievementID;
    public int QuestID;
    public int OverrideArchive; // -1: allow any, otherwise must match OverrideArchive cvar
    public uint ItemModifiedAppearanceID;

    public ChrCustomizationReqFlag GetFlags() { return (ChrCustomizationReqFlag)Flags; }
}