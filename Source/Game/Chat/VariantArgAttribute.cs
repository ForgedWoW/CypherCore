﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Game.Chat;

[AttributeUsage(AttributeTargets.Parameter)]
public class VariantArgAttribute : Attribute
{
	public Type[] Types { get; set; }

	public VariantArgAttribute(params Type[] types)
	{
		Types = types;
	}
}