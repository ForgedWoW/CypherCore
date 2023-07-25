using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SummonPropertiesRecord
{
    public uint Id;
    public SummonCategory Control;
    public uint Faction;
    public SummonTitle Title;
    public int Slot;
    public uint[] Flags = new uint[2];

    public SummonPropertiesFlags GetFlags() { return (SummonPropertiesFlags)Flags[0]; }
}