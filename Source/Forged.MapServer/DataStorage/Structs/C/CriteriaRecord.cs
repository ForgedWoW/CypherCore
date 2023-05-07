// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CriteriaRecord
{
    public uint Asset;
    public ushort EligibilityWorldStateID;
    public byte EligibilityWorldStateValue;
    public uint FailAsset;
    public byte FailEvent;
    public byte Flags;
    public uint Id;
    public uint ModifierTreeId;
    public uint StartAsset;
    public byte StartEvent;
    public ushort StartTimer;
    public CriteriaType Type;
    public CriteriaFlags GetFlags() => (CriteriaFlags)Flags;
}