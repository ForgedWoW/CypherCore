// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.E;

public sealed class EmotesRecord
{
    public int AnimId;
    public int ClassMask;
    public uint EmoteFlags;
    public string EmoteSlashCommand;
    public byte EmoteSpecProc;
    public uint EmoteSpecProcParam;
    public uint EventSoundID;
    public uint Id;
    public long RaceMask;
    public uint SpellVisualKitId;
}