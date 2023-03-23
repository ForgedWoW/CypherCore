using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.E;

public sealed class EmotesRecord
{
	public uint Id;
	public long RaceMask;
	public string EmoteSlashCommand;
	public int AnimId;
	public uint EmoteFlags;
	public byte EmoteSpecProc;
	public uint EmoteSpecProcParam;
	public uint EventSoundID;
	public uint SpellVisualKitId;
	public int ClassMask;
}
