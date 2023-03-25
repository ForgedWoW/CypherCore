// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.F;

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