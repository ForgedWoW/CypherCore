// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Networking;

public interface ISocket
{
	void Accept();
	bool Update();
	bool IsOpen();
	void CloseSocket();
}