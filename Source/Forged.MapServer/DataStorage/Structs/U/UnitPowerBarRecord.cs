// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed class UnitPowerBarRecord
{
    public uint Id;
    public string Name;
    public string Cost;
    public string OutOfError;
    public string ToolTip;
    public uint MinPower;
    public uint MaxPower;
    public uint StartPower;
    public byte CenterPower;
    public float RegenerationPeace;
    public float RegenerationCombat;
    public byte BarType;
    public ushort Flags;
    public float StartInset;
    public float EndInset;
    public uint[] FileDataID = new uint[6];
    public uint[] Color = new uint[6];
}