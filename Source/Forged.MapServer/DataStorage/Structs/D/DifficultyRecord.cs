// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.D;

public sealed class DifficultyRecord
{
    public uint Id;
    public string Name;
    public MapTypes InstanceType;
    public byte OrderIndex;
    public sbyte OldEnumValue;
    public byte FallbackDifficultyID;
    public byte MinPlayers;
    public byte MaxPlayers;
    public DifficultyFlags Flags;
    public byte ItemContext;
    public byte ToggleDifficultyID;
    public ushort GroupSizeHealthCurveID;
    public ushort GroupSizeDmgCurveID;
    public ushort GroupSizeSpellPointsCurveID;
}