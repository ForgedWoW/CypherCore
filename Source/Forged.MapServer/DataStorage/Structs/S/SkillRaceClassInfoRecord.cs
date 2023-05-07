// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SkillRaceClassInfoRecord
{
    public sbyte Availability;
    public int ClassMask;
    public SkillRaceClassInfoFlags Flags;
    public uint Id;
    public sbyte MinLevel;
    public long RaceMask;
    public ushort SkillID;
    public ushort SkillTierID;
}