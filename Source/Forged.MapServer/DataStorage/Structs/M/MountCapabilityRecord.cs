// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed class MountCapabilityRecord
{
    public MountCapabilityFlags Flags;
    public int FlightCapabilityID;
    public uint Id;
    public uint ModSpellAuraID;
    public int PlayerConditionID;
    public ushort ReqAreaID;
    public short ReqMapID;
    public ushort ReqRidingSkill;
    public uint ReqSpellAuraID;
    public uint ReqSpellKnownID;
}