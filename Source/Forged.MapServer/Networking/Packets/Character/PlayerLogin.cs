// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Character;

public class PlayerLogin : ClientPacket
{
	public ObjectGuid Guid; // Guid of the player that is logging in
	public float FarClip;   // Visibility distance (for terrain)
	public PlayerLogin(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		FarClip = _worldPacket.ReadFloat();
	}
}