// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.K;

public sealed class KeystoneAffixRecord
{
	public LocalizedString Name;
	public LocalizedString Description;
	public uint Id;
	public int FiledataID;
}