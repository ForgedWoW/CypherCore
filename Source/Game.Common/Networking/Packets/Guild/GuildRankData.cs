// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Guild;

public class GuildRankData
{
	public byte RankID;
	public uint RankOrder;
	public uint Flags;
	public uint WithdrawGoldLimit;
	public string RankName;
	public uint[] TabFlags = new uint[GuildConst.MaxBankTabs];
	public uint[] TabWithdrawItemLimit = new uint[GuildConst.MaxBankTabs];

	public void Write(WorldPacket data)
	{
		data.WriteUInt8(RankID);
		data.WriteUInt32(RankOrder);
		data.WriteUInt32(Flags);
		data.WriteUInt32(WithdrawGoldLimit);

		for (byte i = 0; i < GuildConst.MaxBankTabs; i++)
		{
			data.WriteUInt32(TabFlags[i]);
			data.WriteUInt32(TabWithdrawItemLimit[i]);
		}

		data.WriteBits(RankName.GetByteCount(), 7);
		data.WriteString(RankName);
	}
}
