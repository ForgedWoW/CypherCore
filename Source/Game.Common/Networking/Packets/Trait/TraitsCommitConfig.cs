// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Trait;

public class TraitsCommitConfig : ClientPacket
{
	public TraitConfigPacket Config = new();
	public int SavedConfigID;
	public int SavedLocalIdentifier;

	public TraitsCommitConfig(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Config.Read(_worldPacket);
		SavedConfigID = _worldPacket.ReadInt32();
		SavedLocalIdentifier = _worldPacket.ReadInt32();
	}
}
