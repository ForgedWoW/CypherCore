﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class BattlePetJournalLockDenied : ServerPacket
{
	public BattlePetJournalLockDenied() : base(ServerOpcodes.BattlePetJournalLockDenied) { }

	public override void Write() { }
}