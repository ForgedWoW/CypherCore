using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.P;

public sealed record PhaseRecord
{
    public uint Id;
    public PhaseEntryFlags Flags;
}