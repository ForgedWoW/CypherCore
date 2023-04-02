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
        Hdr.Write(_worldPacket);
        _worldPacket.WriteUInt32(Mapid);
        _worldPacket.WriteUInt32(ShutdownTimer);
        _worldPacket.WriteUInt32(StartTimer);
        _worldPacket.WriteBit(ArenaFaction != 0);
        _worldPacket.WriteBit(LeftEarly);
        _worldPacket.FlushBits();
    }
}