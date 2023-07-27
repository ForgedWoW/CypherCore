using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed record MountCapabilityRecord
{
    public uint Id;
    public MountCapabilityFlags Flags;
    public ushort ReqRidingSkill;
    public ushort ReqAreaID;
    public uint ReqSpellAuraID;
    public uint ReqSpellKnownID;
    public uint ModSpellAuraID;
    public short ReqMapID;
    public int PlayerConditionID;
    public int FlightCapabilityID;
}