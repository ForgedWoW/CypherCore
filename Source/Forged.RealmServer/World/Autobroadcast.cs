// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer;

struct Autobroadcast
{
	public Autobroadcast(string message, byte weight)
	{
		Message = message;
		Weight = weight;
	}

	public string Message;
	public byte Weight;
}