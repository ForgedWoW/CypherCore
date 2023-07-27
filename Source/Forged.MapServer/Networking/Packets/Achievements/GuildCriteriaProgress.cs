// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Achievements;

public struct GuildCriteriaProgress
{
    public uint CriteriaID;
    public long DateCreated;
    public long DateStarted;
    public long DateUpdated;
    public int Flags;
    public ObjectGuid PlayerGUID;
    public ulong Quantity;
    public int Unused_10_1_5;
}