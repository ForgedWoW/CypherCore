// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Spell;

public class SpellVisualLoadScreen : ServerPacket
{
	public int SpellVisualKitID;
	public int Delay;

	public SpellVisualLoadScreen(int spellVisualKitId, int delay) : base(ServerOpcodes.SpellVisualLoadScreen, ConnectionType.Instance)
	{
		SpellVisualKitID = spellVisualKitId;
		Delay = delay;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(SpellVisualKitID);
		_worldPacket.WriteInt32(Delay);
	}
}
