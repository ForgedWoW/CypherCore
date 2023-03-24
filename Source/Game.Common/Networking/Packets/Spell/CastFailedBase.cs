// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Spell;

public class CastFailedBase : ServerPacket
{
	public ObjectGuid CastID;
	public int SpellID;
	public SpellCastResult Reason;
	public int FailedArg1 = -1;
	public int FailedArg2 = -1;

	public CastFailedBase(ServerOpcodes opcode, ConnectionType connectionType) : base(opcode, connectionType) { }

	public override void Write()
	{
		throw new NotImplementedException();
	}
}
