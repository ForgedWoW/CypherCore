// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Cryptography.Ed25519.Internal.Ed25519Ref10;

internal static partial class GroupOperations
{
    public static void ge_p3_tobytes(byte[] s, int offset, ref GroupElementP3 h)
    {
        FieldOperations.fe_invert(out var recip, ref h.Z);
        FieldOperations.fe_mul(out var x, ref h.X, ref recip);
        FieldOperations.fe_mul(out var y, ref h.Y, ref recip);
        FieldOperations.fe_tobytes(s, offset, ref y);
        s[offset + 31] ^= (byte)(FieldOperations.fe_isnegative(ref x) << 7);
    }
}