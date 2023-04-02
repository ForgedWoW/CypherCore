// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.H;

public sealed class HolidaysRecord
{
    public sbyte CalendarFilterType;
    public byte[] CalendarFlags = new byte[SharedConst.MaxHolidayFlags];
    public uint[] Date = new uint[SharedConst.MaxHolidayDates];
    public ushort[] Duration = new ushort[SharedConst.MaxHolidayDurations];
    public byte Flags;
    public uint HolidayDescriptionID;
    public uint HolidayNameID;
    public uint Id;
    public byte Looping;
    public byte Priority;
    public ushort Region;
    // dates in unix time starting at January, 1, 2000
    public int[] TextureFileDataID = new int[3];
}