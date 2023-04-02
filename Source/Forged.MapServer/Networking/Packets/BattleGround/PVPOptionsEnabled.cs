// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class PVPOptionsEnabled : ServerPacket
{
    public bool ArenaSkirmish;
    public bool PugBattlegrounds;
    public bool RatedArenas;
    public bool RatedBattlegrounds;
    public bool WargameArenas;
    public bool WargameBattlegrounds;
    public PVPOptionsEnabled() : base(ServerOpcodes.PvpOptionsEnabled) { }

    public override void Write()
    {
        _worldPacket.WriteBit(RatedBattlegrounds);
        _worldPacket.WriteBit(PugBattlegrounds);
        _worldPacket.WriteBit(WargameBattlegrounds);
        _worldPacket.WriteBit(WargameArenas);
        _worldPacket.WriteBit(RatedArenas);
        _worldPacket.WriteBit(ArenaSkirmish);
        _worldPacket.FlushBits();
    }
}