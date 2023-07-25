using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class AdventureMapPOIRecord
{
    public uint Id;
    public LocalizedString Title;
    public string Description;
    public Vector2 WorldPosition;
    public sbyte Type;
    public uint PlayerConditionID;
    public uint QuestID;
    public uint LfgDungeonID;
    public int RewardItemID;
    public uint UiTextureAtlasMemberID;
    public uint UiTextureKitID;
    public int MapID;
    public uint AreaTableID;
}