// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Hotfix;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class HotfixHandler : IWorldSessionHandler
{
    private readonly DB2Manager _db2Manager;
    private readonly CliDB _cliDB;
    private readonly WorldSession _session;

    public HotfixHandler(WorldSession session, DB2Manager db2Manager, CliDB cliDB)
    {
        _session = session;
        _db2Manager = db2Manager;
        _cliDB = cliDB;
    }

    [WorldPacketHandler(ClientOpcodes.DbQueryBulk, Processing = PacketProcessing.Inplace, Status = SessionStatus.Authed)]
    private void HandleDBQueryBulk(DBQueryBulk dbQuery)
    {
        _cliDB.Storage.TryGetValue(dbQuery.TableHash, out var store);

        foreach (var record in dbQuery.Queries)
        {
            DBReply dbReply = new()
            {
                TableHash = dbQuery.TableHash,
                RecordID = record.RecordID
            };

            if (store != null && store.HasRecord(record.RecordID))
            {
                dbReply.Status = HotfixRecord.Status.Valid;
                dbReply.Timestamp = (uint)GameTime.CurrentTime;
                store.WriteRecord(record.RecordID, _session.SessionDbcLocale, dbReply.Data);

                var optionalDataEntries = _db2Manager.GetHotfixOptionalData(dbQuery.TableHash, record.RecordID, _session.SessionDbcLocale);

                foreach (var optionalData in optionalDataEntries)
                {
                    dbReply.Data.WriteUInt32(optionalData.Key);
                    dbReply.Data.WriteBytes(optionalData.Data);
                }
            }
            else
            {
                Log.Logger.Verbose("CMSG_DB_QUERY_BULK: {0} requested non-existing entry {1} in datastore: {2}", _session.GetPlayerInfo(), record.RecordID, dbQuery.TableHash);
                dbReply.Timestamp = (uint)GameTime.CurrentTime;
            }

            _session.SendPacket(dbReply);
        }
    }

    [WorldPacketHandler(ClientOpcodes.HotfixRequest, Status = SessionStatus.Authed)]
    private void HandleHotfixRequest(HotfixRequest hotfixQuery)
    {
        var hotfixes = _db2Manager.GetHotfixData();

        HotfixConnect hotfixQueryResponse = new();

        foreach (var hotfixId in hotfixQuery.Hotfixes)
            if (hotfixes.TryGetValue(hotfixId, out var hotfixRecords))
                foreach (var hotfixRecord in hotfixRecords)
                {
                    HotfixConnect.HotfixData hotfixData = new()
                    {
                        Record = hotfixRecord
                    };

                    if (hotfixRecord.HotfixStatus == HotfixRecord.Status.Valid)
                    {
                        if (_cliDB.Storage.TryGetValue(hotfixRecord.TableHash, out var storage) && storage.HasRecord((uint)hotfixRecord.RecordID))
                        {
                            var pos = hotfixQueryResponse.HotfixContent.GetSize();
                            storage.WriteRecord((uint)hotfixRecord.RecordID, _session.SessionDbcLocale, hotfixQueryResponse.HotfixContent);

                            var optionalDataEntries = _db2Manager.GetHotfixOptionalData(hotfixRecord.TableHash, (uint)hotfixRecord.RecordID, _session.SessionDbcLocale);

                            if (optionalDataEntries != null)
                                foreach (var optionalData in optionalDataEntries)
                                {
                                    hotfixQueryResponse.HotfixContent.WriteUInt32(optionalData.Key);
                                    hotfixQueryResponse.HotfixContent.WriteBytes(optionalData.Data);
                                }

                            hotfixData.Size = hotfixQueryResponse.HotfixContent.GetSize() - pos;
                        }
                        else
                        {
                            var blobData = _db2Manager.GetHotfixBlobData(hotfixRecord.TableHash, hotfixRecord.RecordID, _session.SessionDbcLocale);

                            if (blobData != null)
                            {
                                hotfixData.Size = (uint)blobData.Length;
                                hotfixQueryResponse.HotfixContent.WriteBytes(blobData);
                            }
                            else
                                // Do not send Status::Valid when we don't have a hotfix blob for current locale
                                hotfixData.Record.HotfixStatus = storage != null ? HotfixRecord.Status.RecordRemoved : HotfixRecord.Status.Invalid;
                        }
                    }

                    hotfixQueryResponse.Hotfixes.Add(hotfixData);
                }

        _session.SendPacket(hotfixQueryResponse);
    }
}