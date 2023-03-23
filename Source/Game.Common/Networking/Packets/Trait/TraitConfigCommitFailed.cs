﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Trait;

public class TraitConfigCommitFailed : ServerPacket
{
	public int ConfigID;
	public uint SpellID;
	public int Reason;

	public TraitConfigCommitFailed(int configId = 0, uint spellId = 0, int reason = 0) : base(ServerOpcodes.TraitConfigCommitFailed)
	{
		ConfigID = configId;
		SpellID = spellId;
		Reason = reason;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(ConfigID);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBits(Reason, 4);
		_worldPacket.FlushBits();
	}
}
