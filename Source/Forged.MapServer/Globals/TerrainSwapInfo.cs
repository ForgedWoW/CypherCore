using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class TerrainSwapInfo
{
    public uint Id;
    public List<uint> UiMapPhaseIDs = new();
    public TerrainSwapInfo() { }

    public TerrainSwapInfo(uint id)
    {
        Id = id;
    }
}