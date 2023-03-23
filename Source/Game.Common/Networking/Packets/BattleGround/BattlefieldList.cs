// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.BattleGround;

public class BattlefieldList : ServerPacket
{
	public ObjectGuid BattlemasterGuid;
	public int BattlemasterListID;
	public byte MinLevel;
	public byte MaxLevel;
	public List<int> Battlefields = new(); // Players cannot join a specific Battleground instance anymore - this is always empty
	public bool PvpAnywhere;
	public bool HasRandomWinToday;
	public BattlefieldList() : base(ServerOpcodes.BattlefieldList) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(BattlemasterGuid);
		_worldPacket.WriteInt32(BattlemasterListID);
		_worldPacket.WriteUInt8(MinLevel);
		_worldPacket.WriteUInt8(MaxLevel);
		_worldPacket.WriteInt32(Battlefields.Count);

		foreach (var field in Battlefields)
			_worldPacket.WriteInt32(field);

		_worldPacket.WriteBit(PvpAnywhere);
		_worldPacket.WriteBit(HasRandomWinToday);
		_worldPacket.FlushBits();
	}
}
