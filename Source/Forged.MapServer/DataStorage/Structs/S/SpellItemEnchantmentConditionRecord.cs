namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellItemEnchantmentConditionRecord
{
    public uint Id;
    public byte[] LtOperandType = new byte[5];
    public uint[] LtOperand = new uint[5];
    public byte[] Operator = new byte[5];
    public byte[] RtOperandType = new byte[5];
    public byte[] RtOperand = new byte[5];
    public byte[] Logic = new byte[5];
}