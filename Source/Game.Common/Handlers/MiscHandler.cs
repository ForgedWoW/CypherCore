// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Text;
using Framework.Constants;
using Framework.IO;
using Game.Common.DataStorage.Structs.U;
using Game.Common.Entities.Objects;
using Game.Common.Globals;
using Game.Common.Networking;
using Game.Common.Networking.Packets.AreaTrigger;
using Game.Common.Networking.Packets.Character;
using Game.Common.Networking.Packets.Chat;
using Game.Common.Networking.Packets.ClientConfig;
using Game.Common.Networking.Packets.Misc;
using Game.Common.Networking.Packets.Warden;
using Game.Common.Server;

namespace Game.Common.Handlers;

public class MiscHandler
{
    private readonly WorldSession _session;

    public MiscHandler(WorldSession session)
    {
        _session = session;
    }

	[WorldPacketHandler(ClientOpcodes.RequestAccountData, Status = SessionStatus.Authed)]
	void HandleRequestAccountData(RequestAccountData request)
	{
		if (request.DataType > AccountDataTypes.Max)
			return;

		var adata = _session.GetAccountData(request.DataType);

		UpdateAccountData data = new();
		data.Player = _session.Player ? _session.Player.GUID : ObjectGuid.Empty;
		data.Time = (uint)adata.Time;
		data.DataType = request.DataType;

		if (!adata.Data.IsEmpty())
		{
			data.Size = (uint)adata.Data.Length;
			data.CompressedData = new ByteBuffer(ZLib.Compress(Encoding.UTF8.GetBytes(adata.Data)));
		}

        _session.SendPacket(data);
	}

    [WorldPacketHandler(ClientOpcodes.ChatUnregisterAllAddonPrefixes)]
	void HandleUnregisterAllAddonPrefixes(ChatUnregisterAllAddonPrefixes packet)
	{
        _session.RegisteredAddonPrefixes.Clear();
	}

    [WorldPacketHandler(ClientOpcodes.Warden3Data)]
	void HandleWarden3Data(WardenData packet)
	{
		if (_session.GameWarden == null || packet.Data.GetSize() == 0)
			return;

        _session.GameWarden.DecryptData(packet.Data.GetData());
		var opcode = (WardenOpcodes)packet.Data.ReadUInt8();

		switch (opcode)
		{
			case WardenOpcodes.CmsgModuleMissing:
                _session.GameWarden.SendModuleToClient();

				break;
			case WardenOpcodes.CmsgModuleOk:
				_session.GameWarden.RequestHash();

				break;
			case WardenOpcodes.SmsgCheatChecksRequest:
				_session.GameWarden.HandleData(packet.Data);

				break;
			case WardenOpcodes.CmsgMemChecksResult:
				Log.outDebug(LogFilter.Warden, "NYI WARDEN_CMSG_MEM_CHECKS_RESULT received!");

				break;
			case WardenOpcodes.CmsgHashResult:
				_session.GameWarden.HandleHashResult(packet.Data);
				_session.GameWarden.InitializeModule();

				break;
			case WardenOpcodes.CmsgModuleFailed:
				Log.outDebug(LogFilter.Warden, "NYI WARDEN_CMSG_MODULE_FAILED received!");

				break;
			default:
				Log.outDebug(LogFilter.Warden, "Got unknown warden opcode {0} of size {1}.", opcode, packet.Data.GetSize() - 1);

				break;
		}
	}
}
