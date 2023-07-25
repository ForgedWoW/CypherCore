using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TraitNodeRecord
{
    public uint Id;
    public int TraitTreeID;
    public int PosX;
    public int PosY;
    public sbyte Type;
    public int Flags;

    public TraitNodeType GetNodeType() { return (TraitNodeType)Type; }
}