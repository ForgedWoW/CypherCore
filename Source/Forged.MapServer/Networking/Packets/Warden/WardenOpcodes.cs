// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Warden;

public enum WardenOpcodes
{
	// Client.Server
	CmsgModuleMissing = 0,
	CmsgModuleOk = 1,
	CmsgCheatChecksResult = 2,
	CmsgMemChecksResult = 3, // Only Sent If Mem_Check Bytes Doesn'T Match
	CmsgHashResult = 4,
	CmsgModuleFailed = 5, // This Is Sent When Client Failed To Load Uploaded Module Due To Cache Fail

	// Server.Client
	SmsgModuleUse = 0,
	SmsgModuleCache = 1,
	SmsgCheatChecksRequest = 2,
	SmsgModuleInitialize = 3,
	SmsgMemChecksRequest = 4, // Byte Len; While (!Eof) { Byte Unk(1); Byte Index(++); String Module(Can Be 0); Int Offset; Byte Len; Byte[] Bytes_To_Compare[Len]; }
	SmsgHashRequest = 5
}