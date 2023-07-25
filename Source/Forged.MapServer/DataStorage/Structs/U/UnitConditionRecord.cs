using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed class UnitConditionRecord
{
    public uint Id;
    public byte Flags;
    public byte[] Variable = new byte[8];
    public sbyte[] Op = new sbyte[8];
    public int[] Value = new int[8];

    public UnitConditionFlags GetFlags() { return (UnitConditionFlags)Flags; }
}