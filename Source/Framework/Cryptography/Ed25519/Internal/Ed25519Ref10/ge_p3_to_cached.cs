﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Cryptography.Ed25519.Internal.Ed25519Ref10
{
    internal static partial class GroupOperations
	{
		/*
		r = p
		*/
		public static void ge_p3_to_cached(out GroupElementCached r, ref GroupElementP3 p)
		{
			FieldOperations.fe_add(out r.YplusX, ref p.Y, ref p.X);
			FieldOperations.fe_sub(out r.YminusX, ref p.Y, ref p.X);
			r.Z = p.Z;
			FieldOperations.fe_mul(out r.T2d, ref p.T, ref LookupTables.d2);
		}
	}
}