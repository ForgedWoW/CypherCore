// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AreaTrigger;

internal class AreaTriggerNoCorpse : ServerPacket
{
	public AreaTriggerNoCorpse() : base(ServerOpcodes.AreaTriggerNoCorpse) { }

	public override void Write() { }
}