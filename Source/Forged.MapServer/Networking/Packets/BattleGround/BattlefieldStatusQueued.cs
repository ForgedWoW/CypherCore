// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class BattlefieldStatusQueued : ServerPacket
{
    public bool AsGroup;
    public uint AverageWaitTime;
    public bool EligibleForMatchmaking;
    public BattlefieldStatusHeader Hdr = new();
    public bool SuspendedQueue;
    public int Unused920;
    public uint WaitTime;
    public BattlefieldStatusQueued() : base(ServerOpcodes.BattlefieldStatusQueued) { }

    public override void Write()
    {
        Hdr.Write(WorldPacket);
        WorldPacket.WriteUInt32(AverageWaitTime);
        WorldPacket.WriteUInt32(WaitTime);
        WorldPacket.WriteInt32(Unused920);
        WorldPacket.WriteBit(AsGroup);
        WorldPacket.WriteBit(EligibleForMatchmaking);
        WorldPacket.WriteBit(SuspendedQueue);
        WorldPacket.FlushBits();
    }
}