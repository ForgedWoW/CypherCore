// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;

namespace Forged.MapServer.Entities.Items;

[StructLayout(LayoutKind.Sequential)]
public class ItemDynamicFieldGems
{
    public uint ItemId;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public ushort[] BonusListIDs = new ushort[16];

    public byte Context;
}