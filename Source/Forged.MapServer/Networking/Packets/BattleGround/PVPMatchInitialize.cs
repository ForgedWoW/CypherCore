// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class PVPMatchInitialize : ServerPacket
{
    public enum MatchState
    {
        InProgress = 1,
        Complete = 3,
        Inactive = 4
    }

    public bool AffectsRating;

    public byte ArenaFaction;

    public uint BattlemasterListID;

    public RatedMatchDeserterPenalty DeserterPenalty;

    public int Duration;

    public uint MapID;

    public bool Registered;

    public long StartTime;

    public MatchState State = MatchState.Inactive;

    public PVPMatchInitialize() : base(ServerOpcodes.PvpMatchInitialize, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(MapID);
        WorldPacket.WriteUInt8((byte)State);
        WorldPacket.WriteInt64(StartTime);
        WorldPacket.WriteInt32(Duration);
        WorldPacket.WriteUInt8(ArenaFaction);
        WorldPacket.WriteUInt32(BattlemasterListID);
        WorldPacket.WriteBit(Registered);
        WorldPacket.WriteBit(AffectsRating);
        WorldPacket.WriteBit(DeserterPenalty != null);
        WorldPacket.FlushBits();

        DeserterPenalty?.Write(WorldPacket);
    }
}