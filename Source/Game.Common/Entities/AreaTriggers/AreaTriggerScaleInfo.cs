// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Game.Common.Entities.AreaTriggers;

/// 6 bool is_override, only valid for AREATRIGGER_OVERRIDE_SCALE_CURVE, if true then use data from AREATRIGGER_OVERRIDE_SCALE_CURVE instead of ScaleCurveId from CreateObject
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public class AreaTriggerScaleInfo
{
	[FieldOffset(0)] public StructuredData Structured;

	[FieldOffset(0)] public RawData Raw;

	[StructLayout(LayoutKind.Explicit)]
	public struct StructuredData
	{
		[FieldOffset(0)] public uint StartTimeOffset;

		[FieldOffset(4)] public float X;

		[FieldOffset(8)] public float Y;

		[FieldOffset(12)] public float Z;

		[FieldOffset(16)] public float W;

		[FieldOffset(20)] public uint CurveParameters;

		[FieldOffset(24)] public uint OverrideActive;

		public struct curveparameters
		{
			public uint Raw;

			public uint NoData
			{
				get { return Raw & 1; }
			}

			public uint InterpolationMode
			{
				get { return (Raw & 0x7) << 1; }
			}

			public uint FirstPointOffset
			{
				get { return (Raw & 0x7FFFFF) << 4; }
			}

			public uint PointCount
			{
				get { return (Raw & 0x1F) << 27; }
			}
		}
	}

	public unsafe struct RawData
	{
		public fixed uint Data[SharedConst.MaxAreatriggerScale];
	}
}
