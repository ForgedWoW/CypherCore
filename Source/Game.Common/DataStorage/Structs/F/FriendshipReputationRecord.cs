// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.DataStorage.ClientReader;
using Game.DataStorage;

namespace Game.Common.DataStorage.Structs.F;

public sealed class FriendshipReputationRecord
{
	public LocalizedString Description;
	public LocalizedString StandingModified;
	public LocalizedString StandingChanged;
	public uint Id;
	public int FactionID;
	public int TextureFileID;
	public FriendshipReputationFlags Flags;
}
