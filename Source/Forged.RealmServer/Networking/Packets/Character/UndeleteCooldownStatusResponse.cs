// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class UndeleteCooldownStatusResponse : ServerPacket
{
	public bool OnCooldown;      //
	public uint MaxCooldown;     // Max. cooldown until next free character restoration. Displayed in undelete confirm message. (in sec)
	public uint CurrentCooldown; // Current cooldown until next free character restoration. (in sec)
	public UndeleteCooldownStatusResponse() : base(ServerOpcodes.UndeleteCooldownStatusResponse) { }

	public override void Write()
	{
		_worldPacket.WriteBit(OnCooldown);
		_worldPacket.WriteUInt32(MaxCooldown);
		_worldPacket.WriteUInt32(CurrentCooldown);
	}
}