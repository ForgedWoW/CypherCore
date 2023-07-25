using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GameObjectsRecord
{
    public LocalizedString Name;
    public Vector3 Pos;
    public float[] Rot = new float[4];
    public uint Id;
    public uint OwnerID;
    public uint DisplayID;
    public float Scale;
    public GameObjectTypes TypeID;
    public int PhaseUseFlags;
    public int PhaseID;
    public int PhaseGroupID;
    public int[] PropValue = new int[8];
}