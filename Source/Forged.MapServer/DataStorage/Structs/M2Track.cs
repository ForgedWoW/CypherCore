namespace Forged.MapServer.DataStorage.Structs;

public struct M2Track
{
    public ushort interpolation_type;
    public ushort global_sequence;
    public M2Array timestamps;
    public M2Array values;
}