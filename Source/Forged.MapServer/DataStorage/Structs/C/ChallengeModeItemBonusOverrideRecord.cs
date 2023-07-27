namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ChallengeModeItemBonusOverrideRecord
{
    public uint Id;
    public int ItemBonusTreeGroupID;
    public int DstItemBonusTreeID;
    public sbyte Type;
    public int Value;
    public int MythicPlusSeasonID;
    public int PvPSeasonID;
    public uint SrcItemBonusTreeID;
}