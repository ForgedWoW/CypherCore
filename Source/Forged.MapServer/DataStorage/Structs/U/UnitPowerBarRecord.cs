// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed record UnitPowerBarRecord
{
    public byte BarType;
    public byte CenterPower;
    public uint[] Color = new uint[6];
    public string Cost;
    public float EndInset;
    public uint[] FileDataID = new uint[6];
    public ushort Flags;
    public uint Id;
    public uint MaxPower;
    public uint MinPower;
    public string Name;
    public string OutOfError;
    public float RegenerationCombat;
    public float RegenerationPeace;
    public float StartInset;
    public uint StartPower;
    public string ToolTip;
}