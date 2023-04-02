// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class PVPMatchInitialize : ServerPacket
{
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

    public enum MatchState
    {
        InProgress = 1,
        Complete = 3,
        Inactive = 4
    }
    public override void Write()
    {
        _worldPacket.WriteUInt32(MapID);
        _worldPacket.WriteUInt8((byte)State);
        _worldPacket.WriteInt64(StartTime);
        _worldPacket.WriteInt32(Duration);
        _worldPacket.WriteUInt8(ArenaFaction);
        _worldPacket.WriteUInt32(BattlemasterListID);
        _worldPacket.WriteBit(Registered);
        _worldPacket.WriteBit(AffectsRating);
        _worldPacket.WriteBit(DeserterPenalty != null);
        _worldPacket.FlushBits();

        DeserterPenalty?.Write(_worldPacket);
    }
}