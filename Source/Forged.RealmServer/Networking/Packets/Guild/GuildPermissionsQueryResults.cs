// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class GuildPermissionsQueryResults : ServerPacket
{
	public int NumTabs;
	public int WithdrawGoldLimit;
	public int Flags;
	public uint RankID;
	public List<GuildRankTabPermissions> Tab;

	public GuildPermissionsQueryResults() : base(ServerOpcodes.GuildPermissionsQueryResults)
	{
		Tab = new List<GuildRankTabPermissions>();
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(RankID);
		_worldPacket.WriteInt32(WithdrawGoldLimit);
		_worldPacket.WriteInt32(Flags);
		_worldPacket.WriteInt32(NumTabs);
		_worldPacket.WriteInt32(Tab.Count);

		foreach (var tab in Tab)
		{
			_worldPacket.WriteInt32(tab.Flags);
			_worldPacket.WriteInt32(tab.WithdrawItemLimit);
		}
	}

	public struct GuildRankTabPermissions
	{
		public int Flags;
		public int WithdrawItemLimit;
	}
}