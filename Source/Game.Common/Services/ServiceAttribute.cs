// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Common.Services;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ServiceAttribute : Attribute
{
	public uint ServiceHash { get; set; }
	public uint MethodId { get; set; }

	public ServiceAttribute(OriginalHash serviceHash, uint methodId)
	{
		ServiceHash = (uint)serviceHash;
		MethodId = methodId;
	}
}
