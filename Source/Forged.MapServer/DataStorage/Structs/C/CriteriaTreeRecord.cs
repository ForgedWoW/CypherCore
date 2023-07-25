using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CriteriaTreeRecord
{
    public uint Id;
    public string Description;
    public uint Parent;
    public uint Amount;
    public sbyte Operator;
    public uint CriteriaID;
    public int OrderIndex;
    public CriteriaTreeFlags Flags;
}