using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.L;

public sealed class LockRecord
{
    public uint Id;
    public int Flags;
    public int[] Index = new int[SharedConst.MaxLockCase];
    public ushort[] Skill = new ushort[SharedConst.MaxLockCase];
    public byte[] LockType = new byte[SharedConst.MaxLockCase];
    public byte[] Action = new byte[SharedConst.MaxLockCase];
}