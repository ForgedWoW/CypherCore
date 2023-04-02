// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Objects.Update;

public class UpdateFieldString : IUpdateField<string>
{
    public UpdateFieldString(int blockBit, int bit)
    {
        BlockBit = blockBit;
        Bit = bit;
        Value = "";
    }

    public int Bit { get; set; }
    public int BlockBit { get; set; }
    public string Value { get; set; }
    public static implicit operator string(UpdateFieldString updateField)
    {
        return updateField.Value;
    }
}