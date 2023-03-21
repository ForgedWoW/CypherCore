// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Framework.Cryptography;
using Framework.IO;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

class WardenWin : Warden
{
	// GUILD is the shortest string that has no client validation (RAID only sends if in a raid group)
	static readonly string _luaEvalPrefix = "local S,T,R=SendAddonMessage,function()";
	static readonly string _luaEvalMidfix = " end R=S and T()if R then S('_TW',";
	static readonly string _luaEvalPostfix = ",'GUILD')end";
	readonly CategoryCheck[] _checks = new CategoryCheck[(int)WardenCheckCategory.Max];

	uint _serverTicks;
	List<ushort> _currentChecks = new();

	public WardenWin()
	{
		foreach (var category in Enum.GetValues<WardenCheckCategory>())
			_checks[(int)category] = new CategoryCheck(Global.WardenCheckMgr.GetAvailableChecks(category).Shuffle().ToList());
	}

	public override void Init(WorldSession session, BigInteger k)
	{
		Session = session;
		// Generate Warden Key
		SessionKeyGenerator WK = new(k.ToByteArray());
		WK.Generate(InputKey, 16);
		WK.Generate(OutputKey, 16);

		Seed = WardenModuleWin.Seed;

		InputCrypto.PrepareKey(InputKey);
		OutputCrypto.PrepareKey(OutputKey);
		Log.outDebug(LogFilter.Warden, "Server side warden for client {0} initializing...", session.AccountId);
		Log.outDebug(LogFilter.Warden, "C->S Key: {0}", InputKey.ToHexString());
		Log.outDebug(LogFilter.Warden, "S->C Key: {0}", OutputKey.ToHexString());
		Log.outDebug(LogFilter.Warden, "  Seed: {0}", Seed.ToHexString());
		Log.outDebug(LogFilter.Warden, "Loading Module...");

		MakeModuleForClient();

		Log.outDebug(LogFilter.Warden, "Module Key: {0}", Module.Key.ToHexString());
		Log.outDebug(LogFilter.Warden, "Module ID: {0}", Module.Id.ToHexString());
		RequestModule();
	}

	public override void InitializeModuleForClient(out ClientWardenModule module)
	{
		// data assign
		module = new ClientWardenModule();
		module.CompressedData = WardenModuleWin.Module;
		module.CompressedSize = (uint)WardenModuleWin.Module.Length;
		module.Key = WardenModuleWin.ModuleKey;
	}

	public override void InitializeModule()
	{
		Log.outDebug(LogFilter.Warden, "Initialize module");

		// Create packet structure
		WardenInitModuleRequest Request = new();
		Request.Command1 = WardenOpcodes.SmsgModuleInitialize;
		Request.Size1 = 20;
		Request.Unk1 = 1;
		Request.Unk2 = 0;
		Request.Type = 1;
		Request.String_library1 = 0;
		Request.Function1[0] = 0x00024F80; // 0x00400000 + 0x00024F80 SFileOpenFile
		Request.Function1[1] = 0x000218C0; // 0x00400000 + 0x000218C0 SFileGetFileSize
		Request.Function1[2] = 0x00022530; // 0x00400000 + 0x00022530 SFileReadFile
		Request.Function1[3] = 0x00022910; // 0x00400000 + 0x00022910 SFileCloseFile

		Request.CheckSumm1 = BuildChecksum(new byte[]
											{
												Request.Unk1
											},
											20);

		Request.Command2 = WardenOpcodes.SmsgModuleInitialize;
		Request.Size2 = 8;
		Request.Unk3 = 4;
		Request.Unk4 = 0;
		Request.String_library2 = 0;
		Request.Function2 = 0x00419D40; // 0x00400000 + 0x00419D40 FrameScript::GetText
		Request.Function2_set = 1;

		Request.CheckSumm2 = BuildChecksum(new byte[]
											{
												Request.Unk2
											},
											8);

		Request.Command3 = WardenOpcodes.SmsgModuleInitialize;
		Request.Size3 = 8;
		Request.Unk5 = 1;
		Request.Unk6 = 1;
		Request.String_library3 = 0;
		Request.Function3 = 0x0046AE20; // 0x00400000 + 0x0046AE20 PerformanceCounter
		Request.Function3_set = 1;

		Request.CheckSumm3 = BuildChecksum(new byte[]
											{
												Request.Unk5
											},
											8);

		Warden3DataServer packet = new();
		packet.Data = EncryptData(Request);
		Session.SendPacket(packet);
	}

	public override void RequestHash()
	{
		Log.outDebug(LogFilter.Warden, "Request hash");

		// Create packet structure
		WardenHashRequest Request = new();
		Request.Command = WardenOpcodes.SmsgHashRequest;
		Request.Seed = Seed;

		Warden3DataServer packet = new();
		packet.Data = EncryptData(Request);
		Session.SendPacket(packet);
	}

	public override void HandleHashResult(ByteBuffer buff)
	{
		// Verify key
		if (buff.ReadBytes(20) != WardenModuleWin.ClientKeySeedHash)
		{
			var penalty = ApplyPenalty();
			Log.outWarn(LogFilter.Warden, "{0} failed hash reply. Action: {0}", Session.GetPlayerInfo(), penalty);

			return;
		}

		Log.outDebug(LogFilter.Warden, "Request hash reply: succeed");

		// Change keys here
		InputKey = WardenModuleWin.ClientKeySeed;
		OutputKey = WardenModuleWin.ServerKeySeed;

		InputCrypto.PrepareKey(InputKey);
		OutputCrypto.PrepareKey(OutputKey);

		Initialized = true;
	}

	public override void RequestChecks()
	{
		Log.outDebug(LogFilter.Warden, $"Request data from {Session.PlayerName} (account {Session.AccountId}) - loaded: {Session.Player && !Session.PlayerLoading}");

		// If all checks for a category are done, fill its todo list again
		foreach (var category in Enum.GetValues<WardenCheckCategory>())
		{
			var checks = _checks[(int)category];

			if (checks.IsAtEnd() && !checks.Empty())
			{
				Log.outDebug(LogFilter.Warden, $"Finished all {category} checks, re-shuffling");
				checks.Shuffle();
			}
		}

		_serverTicks = GameTime.GetGameTimeMS();
		_currentChecks.Clear();

		// Build check request
		ByteBuffer buff = new();
		buff.WriteUInt8((byte)WardenOpcodes.SmsgCheatChecksRequest);

		foreach (var category in Enum.GetValues<WardenCheckCategory>())
		{
			if (WardenCheckManager.IsWardenCategoryInWorldOnly(category) && !Session.Player)
				continue;

			var checks = _checks[(int)category];

			for (uint i = 0, n = WorldConfig.GetUIntValue(WardenCheckManager.GetWardenCategoryCountConfig(category)); i < n; ++i)
			{
				if (checks.IsAtEnd()) // all checks were already sent, list will be re-filled on next Update() run
					break;

				_currentChecks.Add(checks.CurrentIndex++);
			}
		}

		_currentChecks = _currentChecks.Shuffle().ToList();

		ushort expectedSize = 4;

		_currentChecks.RemoveAll(id =>
		{
			var thisSize = GetCheckPacketSize(Global.WardenCheckMgr.GetCheckData(id));

			if ((expectedSize + thisSize) > 450) // warden packets are truncated to 512 bytes clientside
				return true;

			expectedSize += thisSize;

			return false;
		});

		foreach (var id in _currentChecks)
		{
			var check = Global.WardenCheckMgr.GetCheckData(id);

			if (check.Type == WardenCheckType.LuaEval)
			{
				buff.WriteUInt8((byte)(_luaEvalPrefix.Length - 1 + check.Str.Length + _luaEvalMidfix.Length - 1 + check.IdStr.Length + _luaEvalPostfix.Length - 1));
				buff.WriteString(_luaEvalPrefix);
				buff.WriteString(check.Str);
				buff.WriteString(_luaEvalMidfix);
				buff.WriteString(check.IdStr.ToString());
				buff.WriteString(_luaEvalPostfix);
			}
			else if (!check.Str.IsEmpty())
			{
				buff.WriteUInt8((byte)check.Str.GetByteCount());
				buff.WriteString(check.Str);
			}
		}

		var xorByte = InputKey[0];

		// Add TIMING_CHECK
		buff.WriteUInt8(0x00);
		buff.WriteUInt8((byte)((int)WardenCheckType.Timing ^ xorByte));

		byte index = 1;

		foreach (var checkId in _currentChecks)
		{
			var check = Global.WardenCheckMgr.GetCheckData(checkId);

			var type = check.Type;
			buff.WriteUInt8((byte)((int)type ^ xorByte));

			switch (type)
			{
				case WardenCheckType.Mem:
				{
					buff.WriteUInt8(0x00);
					buff.WriteUInt32(check.Address);
					buff.WriteUInt8(check.Length);

					break;
				}
				case WardenCheckType.PageA:
				case WardenCheckType.PageB:
				{
					buff.WriteBytes(check.Data);
					buff.WriteUInt32(check.Address);
					buff.WriteUInt8(check.Length);

					break;
				}
				case WardenCheckType.Mpq:
				case WardenCheckType.LuaEval:
				{
					buff.WriteUInt8(index++);

					break;
				}
				case WardenCheckType.Driver:
				{
					buff.WriteBytes(check.Data);
					buff.WriteUInt8(index++);

					break;
				}
				case WardenCheckType.Module:
				{
					var seed = RandomHelper.Rand32();
					buff.WriteUInt32(seed);
					HmacHash hmac = new(BitConverter.GetBytes(seed));
					hmac.Finish(check.Str);
					buff.WriteBytes(hmac.Digest);

					break;
				}
				/*case PROC_CHECK:
				{
					buff.append(wd.i.AsByteArray(0, false).get(), wd.i.GetNumBytes());
					buff << uint8(index++);
					buff << uint8(index++);
					buff << uint32(wd.Address);
					buff << uint8(wd.Length);
					break;
				}*/
				default:
					break; // Should never happen
			}
		}

		buff.WriteUInt8(xorByte);

		var idstring = "";

		foreach (var id in _currentChecks)
			idstring += $"{id} ";

		if (buff.GetSize() == expectedSize)
		{
			Log.outDebug(LogFilter.Warden, $"Finished building warden packet, size is {buff.GetSize()} bytes");
			Log.outDebug(LogFilter.Warden, $"Sent checks: {idstring}");
		}
		else
		{
			Log.outWarn(LogFilter.Warden, $"Finished building warden packet, size is {buff.GetSize()} bytes, but expected {expectedSize} bytes!");
			Log.outWarn(LogFilter.Warden, $"Sent checks: {idstring}");
		}

		Warden3DataServer packet = new();
		packet.Data = EncryptData(buff.GetData());
		Session.SendPacket(packet);

		DataSent = true;
	}

	public override void HandleCheckResult(ByteBuffer buff)
	{
		Log.outDebug(LogFilter.Warden, "Handle data");

		DataSent = false;
		ClientResponseTimer = 0;

		var Length = buff.ReadUInt16();
		var Checksum = buff.ReadUInt32();

		if (!IsValidCheckSum(Checksum, buff.GetData(), Length))
		{
			var penalty = ApplyPenalty();
			Log.outWarn(LogFilter.Warden, "{0} failed checksum. Action: {1}", Session.GetPlayerInfo(), penalty);

			return;
		}

		// TIMING_CHECK
		{
			var result = buff.ReadUInt8();

			// @todo test it.
			if (result == 0x00)
			{
				var penalty = ApplyPenalty();
				Log.outWarn(LogFilter.Warden, "{0} failed timing check. Action: {1}", Session.GetPlayerInfo(), penalty);

				return;
			}

			var newClientTicks = buff.ReadUInt32();

			var ticksNow = GameTime.GetGameTimeMS();
			var ourTicks = newClientTicks + (ticksNow - _serverTicks);

			Log.outDebug(LogFilter.Warden, "ServerTicks {0}", ticksNow);      // Now
			Log.outDebug(LogFilter.Warden, "RequestTicks {0}", _serverTicks); // At request
			Log.outDebug(LogFilter.Warden, "Ticks {0}", newClientTicks);      // At response
			Log.outDebug(LogFilter.Warden, "Ticks diff {0}", ourTicks - newClientTicks);
		}

		//BigInteger rs;
		//WardenCheck rd;
		// WardenCheckType type; // TODO unused.
		ushort checkFailed = 0;

		foreach (var id in _currentChecks)
		{
			var check = Global.WardenCheckMgr.GetCheckData(id);

			switch (check.Type)
			{
				case WardenCheckType.Mem:
				{
					var result = buff.ReadUInt8();

					if (result != 0)
					{
						Log.outDebug(LogFilter.Warden, $"RESULT MEM_CHECK not 0x00, CheckId {id} account Id {Session.AccountId}");
						checkFailed = id;

						continue;
					}

					var expected = Global.WardenCheckMgr.GetCheckResult(id);

					if (buff.ReadBytes((uint)expected.Length).Compare(expected))
					{
						Log.outDebug(LogFilter.Warden, $"RESULT MEM_CHECK fail CheckId {id} account Id {Session.AccountId}");
						checkFailed = id;

						continue;
					}

					Log.outDebug(LogFilter.Warden, $"RESULT MEM_CHECK passed CheckId {id} account Id {Session.AccountId}");

					break;
				}
				case WardenCheckType.PageA:
				case WardenCheckType.PageB:
				case WardenCheckType.Driver:
				case WardenCheckType.Module:
				{
					if (buff.ReadUInt8() != 0xE9)
					{
						Log.outDebug(LogFilter.Warden, $"RESULT {check.Type} fail, CheckId {id} account Id {Session.AccountId}");
						checkFailed = id;

						continue;
					}

					Log.outDebug(LogFilter.Warden, $"RESULT {check.Type} passed CheckId {id} account Id {Session.AccountId}");

					break;
				}
				case WardenCheckType.LuaEval:
				{
					var result = buff.ReadUInt8();

					if (result == 0)
						buff.Skip(buff.ReadUInt8()); // discard attached string

					Log.outDebug(LogFilter.Warden, $"LUA_EVAL_CHECK CheckId {id} account Id {Session.AccountId} got in-warden dummy response ({result})");

					break;
				}
				case WardenCheckType.Mpq:
				{
					var result = buff.ReadUInt8();

					if (result != 0)
					{
						Log.outDebug(LogFilter.Warden, $"RESULT MPQ_CHECK not 0x00 account id {Session.AccountId}", Session.AccountId);
						checkFailed = id;

						continue;
					}

					if (!buff.ReadBytes(20).Compare(Global.WardenCheckMgr.GetCheckResult(id))) // SHA1
					{
						Log.outDebug(LogFilter.Warden, $"RESULT MPQ_CHECK fail, CheckId {id} account Id {Session.AccountId}");
						checkFailed = id;

						continue;
					}

					Log.outDebug(LogFilter.Warden, $"RESULT MPQ_CHECK passed, CheckId {id} account Id {Session.AccountId}");

					break;
				}
				default: // Should never happen
					break;
			}
		}

		if (checkFailed > 0)
		{
			var check = Global.WardenCheckMgr.GetCheckData(checkFailed);
			var penalty = ApplyPenalty(check);
			Log.outWarn(LogFilter.Warden, $"{Session.GetPlayerInfo()} failed Warden check {checkFailed}. Action: {penalty}");
		}

		// Set hold off timer, minimum timer should at least be 1 second
		var holdOff = WorldConfig.GetUIntValue(WorldCfg.WardenClientCheckHoldoff);
		CheckTimer = (holdOff < 1 ? 1 : holdOff) * Time.InMilliseconds;
	}

	static byte GetCheckPacketBaseSize(WardenCheckType type) => type switch
	{
		WardenCheckType.Driver  => 1,
		WardenCheckType.LuaEval => (byte)(1 + _luaEvalPrefix.Length - 1 + _luaEvalMidfix.Length - 1 + 4 + _luaEvalPostfix.Length - 1),
		WardenCheckType.Mpq     => 1,
		WardenCheckType.PageA   => 4 + 1,
		WardenCheckType.PageB   => 4 + 1,
		WardenCheckType.Module  => 4 + 20,
		WardenCheckType.Mem     => 1 + 4 + 1,
		_                       => 0,
	};

	static ushort GetCheckPacketSize(WardenCheck check)
	{
		var size = 1 + GetCheckPacketBaseSize(check.Type); // 1 byte check type

		if (!check.Str.IsEmpty())
			size += (check.Str.Length + 1); // 1 byte string length

		if (!check.Data.Empty())
			size += check.Data.Length;

		return (ushort)size;
	}
}