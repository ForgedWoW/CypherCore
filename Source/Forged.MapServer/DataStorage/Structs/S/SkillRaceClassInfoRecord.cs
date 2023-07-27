using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SkillRaceClassInfoRecord
{
    public uint Id;
    public long RaceMask;
    public ushort SkillID;
    public int ClassMask;
    public SkillRaceClassInfoFlags Flags;
    public sbyte Availability;
    public sbyte MinLevel;
    public ushort SkillTierID;
}