// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
using Game.Common.Networking.Packets.Spell;

namespace Game.Common.Networking.Packets.Spell;

public class ResyncRunes : ServerPacket
{
	public RuneData Runes = new();
	public ResyncRunes() : base(ServerOpcodes.ResyncRunes) { }

	public override void Write()
	{
		Runes.Write(_worldPacket);
	}
}
