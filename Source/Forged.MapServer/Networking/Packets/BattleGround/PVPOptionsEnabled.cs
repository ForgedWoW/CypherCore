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
        WorldPacket.WriteBit(RatedBattlegrounds);
        WorldPacket.WriteBit(PugBattlegrounds);
        WorldPacket.WriteBit(WargameBattlegrounds);
        WorldPacket.WriteBit(WargameArenas);
        WorldPacket.WriteBit(RatedArenas);
        WorldPacket.WriteBit(ArenaSkirmish);
        WorldPacket.FlushBits();
    }
}