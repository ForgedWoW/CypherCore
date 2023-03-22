﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Game.Chat;

[AttributeUsage(AttributeTargets.Class)]
public class CommandGroupAttribute : CommandAttribute
{
	public CommandGroupAttribute(string command) : base(command) { }
}