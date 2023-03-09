// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class GuildSetRankPermissions : ClientPacket
{
	public byte RankID;
	public int RankOrder;
	public uint WithdrawGoldLimit;
	public uint Flags;
	public uint OldFlags;
	public uint[] TabFlags = new uint[GuildConst.MaxBankTabs];
	public uint[] TabWithdrawItemLimit = new uint[GuildConst.MaxBankTabs];
	public string RankName;
	public GuildSetRankPermissions(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RankID = _worldPacket.ReadUInt8();
		RankOrder = _worldPacket.ReadInt32();
		Flags = _worldPacket.ReadUInt32();
		WithdrawGoldLimit = _worldPacket.ReadUInt32();

		for (byte i = 0; i < GuildConst.MaxBankTabs; i++)
		{
			TabFlags[i] = _worldPacket.ReadUInt32();
			TabWithdrawItemLimit[i] = _worldPacket.ReadUInt32();
		}

		_worldPacket.ResetBitPos();
		var rankNameLen = _worldPacket.ReadBits<uint>(7);

		RankName = _worldPacket.ReadString(rankNameLen);

		OldFlags = _worldPacket.ReadUInt32();
	}
}