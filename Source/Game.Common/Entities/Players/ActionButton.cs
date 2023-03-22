﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Entities;

public class ActionButton
{
	public ActionButtonUpdateState UState;

	public ulong PackedData { get; set; }

	public ActionButton()
	{
		PackedData = 0;
		UState = ActionButtonUpdateState.New;
	}

	public ActionButtonType GetButtonType()
	{
		return (ActionButtonType)((PackedData & 0xFF00000000000000) >> 56);
	}

	public ulong GetAction()
	{
		return (PackedData & 0x00FFFFFFFFFFFFFF);
	}

	public void SetActionAndType(ulong action, ActionButtonType type)
	{
		var newData = action | ((ulong)type << 56);

		if (newData != PackedData || UState == ActionButtonUpdateState.Deleted)
		{
			PackedData = newData;

			if (UState != ActionButtonUpdateState.New)
				UState = ActionButtonUpdateState.Changed;
		}
	}
}