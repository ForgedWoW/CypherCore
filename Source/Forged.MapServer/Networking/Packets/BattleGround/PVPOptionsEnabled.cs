// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

class PVPOptionsEnabled : ServerPacket
{
	public bool WargameArenas;
	public bool RatedArenas;
	public bool WargameBattlegrounds;
	public bool ArenaSkirmish;
	public bool PugBattlegrounds;
	public bool RatedBattlegrounds;
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