// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.D;

public sealed class DifficultyRecord
{
    public byte FallbackDifficultyID;
    public DifficultyFlags Flags;
    public ushort GroupSizeDmgCurveID;
    public ushort GroupSizeHealthCurveID;
    public ushort GroupSizeSpellPointsCurveID;
    public uint Id;
    public MapTypes InstanceType;
    public byte ItemContext;
    public byte MaxPlayers;
    public byte MinPlayers;
    public string Name;
    public sbyte OldEnumValue;
    public byte OrderIndex;
    public byte ToggleDifficultyID;
}