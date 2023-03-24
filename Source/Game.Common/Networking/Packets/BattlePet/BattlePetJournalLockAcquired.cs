﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.BattlePet;

public class BattlePetJournalLockAcquired : ServerPacket
{
	public BattlePetJournalLockAcquired() : base(ServerOpcodes.BattlePetJournalLockAcquired) { }

	public override void Write() { }
}
