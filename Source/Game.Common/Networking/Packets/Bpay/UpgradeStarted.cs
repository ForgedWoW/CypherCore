﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Bpay;

public class UpgradeStarted : ServerPacket
{
	public ObjectGuid CharacterGUID { get; set; } = new();

	public UpgradeStarted() : base(ServerOpcodes.CharacterUpgradeStarted) { }

	public override void Write()
	{
		_worldPacket.Write(CharacterGUID);
	}
}
