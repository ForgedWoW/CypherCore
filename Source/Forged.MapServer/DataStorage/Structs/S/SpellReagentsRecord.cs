using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellReagentsRecord
{
    public uint Id;
    public uint SpellID;
    public int[] Reagent = new int[SpellConst.MaxReagents];
    public ushort[] ReagentCount = new ushort[SpellConst.MaxReagents];
    public short[] ReagentRecraftCount = new short[SpellConst.MaxReagents];
    public byte[] ReagentSource = new byte[SpellConst.MaxReagents];
}