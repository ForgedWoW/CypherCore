// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TransmogSetRecord
{
    public int ClassMask;
    public byte ExpansionID;
    public int Flags;
    public uint Id;
    public int ItemNameDescriptionID;
    public string Name;
    public ushort ParentTransmogSetID;
    public int PatchID;
    public uint PlayerConditionID;
    public uint TrackingQuestID;
    public uint TransmogSetGroupID;
    public short UiOrder;
    public byte Unknown810;
}