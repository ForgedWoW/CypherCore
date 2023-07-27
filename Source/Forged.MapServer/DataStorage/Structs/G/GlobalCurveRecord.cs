using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed record GlobalCurveRecord
{
    public uint Id;
    public uint CurveID;
    public GlobalCurve Type;
}