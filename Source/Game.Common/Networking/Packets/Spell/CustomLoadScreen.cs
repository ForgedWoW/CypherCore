// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Spell;

public class CustomLoadScreen : ServerPacket
{
	readonly uint TeleportSpellID;
	readonly uint LoadingScreenID;

	public CustomLoadScreen(uint teleportSpellId, uint loadingScreenId) : base(ServerOpcodes.CustomLoadScreen)
	{
		TeleportSpellID = teleportSpellId;
		LoadingScreenID = loadingScreenId;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(TeleportSpellID);
		_worldPacket.WriteUInt32(LoadingScreenID);
	}
}
