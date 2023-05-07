// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.J;

public sealed record JournalInstanceRecord
{
    public ushort AreaID;
    public int BackgroundFileDataID;
    public int ButtonFileDataID;
    public int ButtonSmallFileDataID;
    public LocalizedString Description;
    public int Flags;
    public uint Id;
    public int LoreFileDataID;
    public ushort MapID;
    public LocalizedString Name;
}