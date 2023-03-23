using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.C;

public sealed class CinematicSequencesRecord
{
	public uint Id;
	public uint SoundID;
	public ushort[] Camera = new ushort[8];
}
