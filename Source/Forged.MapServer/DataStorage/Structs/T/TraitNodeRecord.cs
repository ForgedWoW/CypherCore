using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitNodeRecord
{
    public uint Id;
    public int TraitTreeID;
    public int PosX;
    public int PosY;
    public sbyte Type;
    public int Flags;

    public TraitNodeType GetNodeType() { return (TraitNodeType)Type; }
}