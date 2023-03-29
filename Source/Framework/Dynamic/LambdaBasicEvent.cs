// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Dynamic;

public class LambdaBasicEvent : BasicEvent
{
    private readonly Action _callback;

	public LambdaBasicEvent(Action callback) : base()
	{
		_callback = callback;
	}

	public override bool Execute(ulong etime, uint pTime)
	{
		_callback();

		return true;
	}
}