// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class BattlefieldStatusActive : ServerPacket
{
    public byte ArenaFaction;
    public BattlefieldStatusHeader Hdr = new();
    public bool LeftEarly;
    public uint Mapid;
    public uint ShutdownTimer;
    public uint StartTimer;
    public BattlefieldStatusActive() : base(ServerOpcodes.BattlefieldStatusActive) { }

    public override void Write()
    {
        Hdr.Write(WorldPacket);
        WorldPacket.WriteUInt32(Mapid);
        WorldPacket.WriteUInt32(ShutdownTimer);
        WorldPacket.WriteUInt32(StartTimer);
        WorldPacket.WriteBit(ArenaFaction != 0);
        WorldPacket.WriteBit(LeftEarly);
        WorldPacket.FlushBits();
    }
}