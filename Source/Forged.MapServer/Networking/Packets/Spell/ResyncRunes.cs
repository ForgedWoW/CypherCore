// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class ResyncRunes : ServerPacket
{
	public RuneData Runes = new();
	public ResyncRunes() : base(ServerOpcodes.ResyncRunes) { }

	public override void Write()
	{
		Runes.Write(_worldPacket);
	}
}