// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using System.Security.Cryptography;
using Forged.MapServer.Accounts;
using Forged.MapServer.Networking.Packets.Warden;
using Forged.MapServer.Server;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Cryptography;
using Framework.IO;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Warden;

public abstract class Warden
{
    internal readonly SARC4 InputCrypto;
    internal readonly SARC4 OutputCrypto;

    internal uint CheckTimer;

    // Timer for sending check requests
    internal uint ClientResponseTimer;

    // Timer for client response delay
    internal bool DataSent;

    internal bool Initialized;
    internal byte[] InputKey = new byte[16];
    internal ClientWardenModule Module;
    internal byte[] OutputKey = new byte[16];
    internal byte[] Seed = new byte[16];
    internal WorldSession Session;

    protected Warden(IConfiguration configuration, AccountManager accountManager, WorldManager worldManager, WardenCheckManager wardenCheckManager)
    {
        Configuration = configuration;
        AccountManager = accountManager;
        WorldManager = worldManager;
        WardenCheckManager = wardenCheckManager;
        InputCrypto = new SARC4();
        OutputCrypto = new SARC4();
        CheckTimer = 10 * Time.IN_MILLISECONDS;
    }

    public IConfiguration Configuration { get; }
    public AccountManager AccountManager { get; }
    public WorldManager WorldManager { get; }
    public WardenCheckManager WardenCheckManager { get; }

    public string ApplyPenalty(WardenCheck check = null)
    {
        WardenActions action;

        if (check != null)
            action = check.Action;
        else
            action = (WardenActions)Configuration.GetDefaultValue("Warden:ClientCheckFailAction", 0);

        switch (action)
        {
            case WardenActions.Kick:
                Session.KickPlayer("Warden::Penalty");

                break;
            case WardenActions.Ban:
            {
                AccountManager.GetName(Session.AccountId, out var accountName);
                var banReason = "Warden Anticheat Violation";

                // Check can be NULL, for example if the client sent a wrong signature in the warden packet (CHECKSUM FAIL)
                if (check != null)
                    banReason += ": " + check.Comment + " (CheckId: " + check.CheckId + ")";

                WorldManager.BanAccount(BanMode.Account, accountName, Configuration.GetDefaultValue("Warden:BanDuration", 86400u), banReason, "Server");

                break;
            }
            case WardenActions.Log:
            default:
                return "None";
        }

        return action.ToString();
    }

    public uint BuildChecksum(byte[] data, uint length)
    {
        var sha = SHA1.Create();

        var hash = sha.ComputeHash(data, 0, (int)length);
        uint checkSum = 0;

        for (byte i = 0; i < 5; ++i)
            checkSum ^= BitConverter.ToUInt32(hash, i * 4);

        return checkSum;
    }

    public void DecryptData(byte[] buffer)
    {
        InputCrypto.ProcessBuffer(buffer, buffer.Length);
    }

    public ByteBuffer EncryptData(byte[] buffer)
    {
        OutputCrypto.ProcessBuffer(buffer, buffer.Length);

        return new ByteBuffer(buffer);
    }

    public abstract void HandleCheckResult(ByteBuffer buff);

    public void HandleData(ByteBuffer buff)
    {
        var data = buff.GetData();
        DecryptData(data);
        var opcode = data[0];
        Log.Logger.Debug($"Got packet, opcode 0x{opcode:X}, size {data.Length - 1}");

        switch ((WardenOpcodes)opcode)
        {
            case WardenOpcodes.CmsgModuleMissing:
                SendModuleToClient();

                break;
            case WardenOpcodes.CmsgModuleOk:
                RequestHash();

                break;
            case WardenOpcodes.CmsgCheatChecksResult:
                HandleCheckResult(buff);

                break;
            case WardenOpcodes.CmsgMemChecksResult:
                Log.Logger.Debug("NYI WARDEN_CMSG_MEM_CHECKS_RESULT received!");

                break;
            case WardenOpcodes.CmsgHashResult:
                HandleHashResult(buff);
                InitializeModule();

                break;
            case WardenOpcodes.CmsgModuleFailed:
                Log.Logger.Debug("NYI WARDEN_CMSG_MODULE_FAILED received!");

                break;
            default:
                Log.Logger.Warning($"Got unknown warden opcode 0x{opcode:X} of size {data.Length - 1}.");

                break;
        }
    }

    public abstract void HandleHashResult(ByteBuffer buff);

    public abstract void Init(WorldSession session, BigInteger k);

    public abstract void InitializeModule();

    public abstract void InitializeModuleForClient(out ClientWardenModule module);

    public bool IsValidCheckSum(uint checksum, byte[] data, ushort length)
    {
        var newChecksum = BuildChecksum(data, length);

        if (checksum != newChecksum)
        {
            Log.Logger.Debug("CHECKSUM IS NOT VALID");

            return false;
        }
        else
        {
            Log.Logger.Debug("CHECKSUM IS VALID");

            return true;
        }
    }

    public void MakeModuleForClient()
    {
        Log.Logger.Debug("Make module for client");
        InitializeModuleForClient(out Module);

        // md5 hash
        var ctx = MD5.Create();
        ctx.Initialize();
        ctx.TransformBlock(Module.CompressedData, 0, Module.CompressedData.Length, Module.CompressedData, 0);
        ctx.TransformBlock(Module.Id, 0, Module.Id.Length, Module.Id, 0);
    }

    public abstract void RequestChecks();

    public abstract void RequestHash();

    public void RequestModule()
    {
        Log.Logger.Debug("Request module");

        // Create packet structure
        WardenModuleUse request = new()
        {
            Command = WardenOpcodes.SmsgModuleUse,
            ModuleId = Module.Id,
            ModuleKey = Module.Key,
            Size = Module.CompressedSize
        };

        Warden3DataServer packet = new()
        {
            Data = EncryptData(request)
        };

        Session.SendPacket(packet);
    }

    public void SendModuleToClient()
    {
        Log.Logger.Debug("Send module to client");

        // Create packet structure
        WardenModuleTransfer packet = new();

        var sizeLeft = Module.CompressedSize;
        var pos = 0;

        while (sizeLeft > 0)
        {
            var burstSize = sizeLeft < 500 ? sizeLeft : 500u;
            packet.Command = WardenOpcodes.SmsgModuleCache;
            packet.DataSize = (ushort)burstSize;
            Buffer.BlockCopy(Module.CompressedData, pos, packet.Data, 0, (int)burstSize);
            sizeLeft -= burstSize;
            pos += (int)burstSize;

            Warden3DataServer pkt1 = new()
            {
                Data = EncryptData(packet)
            };

            Session.SendPacket(pkt1);
        }
    }

    public void Update(uint diff)
    {
        if (!Initialized)
            return;

        if (DataSent)
        {
            var maxClientResponseDelay = Configuration.GetDefaultValue("Warden:ClientResponseDelay", 600u);

            if (maxClientResponseDelay > 0)
            {
                // Kick player if client response delays more than set in config
                if (ClientResponseTimer > maxClientResponseDelay * Time.IN_MILLISECONDS)
                {
                    Log.Logger.Warning("{0} (latency: {1}, IP: {2}) exceeded Warden module response delay for more than {3} - disconnecting client",
                                       Session.GetPlayerInfo(),
                                       Session.Latency,
                                       Session.RemoteAddress,
                                       Time.SecsToTimeString(maxClientResponseDelay, TimeFormat.ShortText));

                    Session.KickPlayer("Warden::Update Warden module response delay exceeded");
                }
                else
                    ClientResponseTimer += diff;
            }
        }
        else
        {
            if (diff >= CheckTimer)
                RequestChecks();
            else
                CheckTimer -= diff;
        }
    }

    private bool ProcessLuaCheckResponse(string msg)
    {
        var wardenToken = "_TW\t";

        if (!msg.StartsWith(wardenToken))
            return false;

        ushort id = 0;
        ushort.Parse(msg.Substring(wardenToken.Length - 1, 10));

        if (id < WardenCheckManager.MaxValidCheckId)
        {
            var check = WardenCheckManager.GetCheckData(id);

            if (check.Type == WardenCheckType.LuaEval)
            {
                var penalty1 = ApplyPenalty(check);
                Log.Logger.Warning($"{Session.GetPlayerInfo()} failed Warden check {id} ({check.Type}). Action: {penalty1}");

                return true;
            }
        }

        var penalty = ApplyPenalty();
        Log.Logger.Warning($"{Session.GetPlayerInfo()} sent bogus Lua check response for Warden. Action: {penalty}");

        return true;
    }
}