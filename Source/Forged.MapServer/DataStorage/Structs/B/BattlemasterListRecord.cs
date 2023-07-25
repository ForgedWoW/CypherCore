using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.B;

public sealed class BattlemasterListRecord
{
    public uint Id;
    public LocalizedString Name;
    public string GameType;
    public string ShortDescription;
    public string LongDescription;
    public sbyte InstanceType;
    public byte MinLevel;
    public byte MaxLevel;
    public sbyte RatedPlayers;
    public sbyte MinPlayers;
    public int MaxPlayers;
    public sbyte GroupsAllowed;
    public sbyte MaxGroupSize;
    public ushort HolidayWorldState;
    public BattlemasterListFlags Flags;
    public int IconFileDataID;
    public int RequiredPlayerConditionID;
    public short[] MapId = new short[16];
}