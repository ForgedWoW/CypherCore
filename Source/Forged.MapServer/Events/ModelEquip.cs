// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Events;

public class ModelEquip
{
    public byte EquipementIDPrev { get; set; }
    public byte EquipmentID { get; set; }
    public uint Modelid { get; set; }
    public uint ModelidPrev { get; set; }
}