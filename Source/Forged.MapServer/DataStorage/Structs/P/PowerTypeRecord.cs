// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.P;

public sealed record PowerTypeRecord
{
    public int CenterPower;
    public string CostGlobalStringTag;
    public int DefaultPower;
    public int DisplayModifier;
    public short Flags;
    public uint Id;
    public int MaxBasePower;
    public int MinPower;
    public string NameGlobalStringTag;
    public PowerType PowerTypeEnum;
    public float RegenCombat;
    public int RegenInterruptTimeMS;
    public float RegenPeace;
}