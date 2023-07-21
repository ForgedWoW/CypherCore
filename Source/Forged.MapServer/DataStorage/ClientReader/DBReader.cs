// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.ClientReader;

internal class DBReader
{
    public ColumnMetaData[] ColumnMeta;
    public Dictionary<int, Value32>[] CommonData;
    public FieldMetaData[] FieldMeta;
    public WDCHeader Header;
    public Value32[][] PalletData;
    public Dictionary<int, WDC4Row> Records = new();
    private const uint Wdc3FmtSig = 0x34434457; // WDC3
    Dictionary<int, int[]> _encryptedIDs;

    public bool Load(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8);

        Header = new WDCHeader
        {
            Signature = reader.ReadUInt32()
        };

        if (Header.Signature != Wdc3FmtSig)
            return false;

        Header.RecordCount = reader.ReadUInt32();
        Header.FieldCount = reader.ReadUInt32();
        Header.RecordSize = reader.ReadUInt32();
        Header.StringTableSize = reader.ReadUInt32();
        Header.TableHash = reader.ReadUInt32();
        Header.LayoutHash = reader.ReadUInt32();
        Header.MinId = reader.ReadInt32();
        Header.MaxId = reader.ReadInt32();
        Header.Locale = reader.ReadInt32();
        Header.Flags = (HeaderFlags)reader.ReadUInt16();
        Header.IdIndex = reader.ReadUInt16();
        Header.TotalFieldCount = reader.ReadUInt32();
        Header.BitpackedDataOffset = reader.ReadUInt32();
        Header.LookupColumnCount = reader.ReadUInt32();
        Header.ColumnMetaSize = reader.ReadUInt32();
        Header.CommonDataSize = reader.ReadUInt32();
        Header.PalletDataSize = reader.ReadUInt32();
        Header.SectionsCount = reader.ReadUInt32();

        var sections = reader.ReadArray<SectionHeader>(Header.SectionsCount);

        // field meta data
        FieldMeta = reader.ReadArray<FieldMetaData>(Header.FieldCount);

        // column meta data 
        ColumnMeta = reader.ReadArray<ColumnMetaData>(Header.FieldCount);

        // pallet data
        PalletData = new Value32[ColumnMeta.Length][];
        for (var i = 0; i < ColumnMeta.Length; i++)
        {
            if (ColumnMeta[i].CompressionType == DB2ColumnCompression.Pallet || ColumnMeta[i].CompressionType == DB2ColumnCompression.PalletArray)
            {
                PalletData[i] = reader.ReadArray<Value32>(ColumnMeta[i].AdditionalDataSize / 4);
            }
        }

        // common data
        CommonData = new Dictionary<int, Value32>[ColumnMeta.Length];
        for (var i = 0; i < ColumnMeta.Length; i++)
        {
            if (ColumnMeta[i].CompressionType != DB2ColumnCompression.Common)
                continue;

            Dictionary<int, Value32> commonValues = new();
            CommonData[i] = commonValues;

            for (var j = 0; j < ColumnMeta[i].AdditionalDataSize / 8; j++)
                commonValues[reader.ReadInt32()] = reader.Read<Value32>();
        }

        // encrypted IDs
        _encryptedIDs = new Dictionary<int, int[]>();
        for (var i = 1; i < Header.SectionsCount; i++)
        {
            var encryptedIDCount = reader.ReadUInt32();

            // If tactkey in section header is 0'd out, skip these IDs
            if (sections[i].TactKeyLookup == 0 || sections[i].TactKeyLookup == 0x5452494E49545900)
                reader.BaseStream.Position += encryptedIDCount * 4;
            else
                _encryptedIDs.Add(i, reader.ReadArray<int>(encryptedIDCount));
        }

        long previousRecordCount = 0;
        foreach (var section in sections)
        {
            reader.BaseStream.Position = section.FileOffset;

            byte[] recordsData;
            Dictionary<long, string> stringsTable = null;
            SparseEntry[] sparseEntries = null;

            if (!Header.HasOffsetTable())
            {
                // records data
                recordsData = reader.ReadBytes((int)(section.NumRecords * Header.RecordSize));

                // string data
                stringsTable = new Dictionary<long, string>();

                for (var i = 0; i < section.StringTableSize;)
                {
                    var oldPos = reader.BaseStream.Position;

                    stringsTable[i] = reader.ReadCString();

                    i += (int)(reader.BaseStream.Position - oldPos);
                }
            }
            else
            {
                // sparse data with inlined strings
                recordsData = reader.ReadBytes(section.OffsetRecordsEndOffset - section.FileOffset);

                if (reader.BaseStream.Position != section.OffsetRecordsEndOffset)
                    throw new Exception("reader.BaseStream.Position != sections[sectionIndex].SparseTableOffset");
            }

            // skip encrypted sections => has tact key + record data is zero filled
            if (section.TactKeyLookup != 0 && Array.TrueForAll(recordsData, x => x == 0))
            {
                var completelyZero = false;
                if (section.IndexDataSize > 0 || section.CopyTableCount > 0)
                {
                    // this will be the record id from m_indexData or m_copyData
                    // if this is zero then the id for this record will be zero which is invalid
                    completelyZero = reader.ReadInt32() == 0;
                    reader.BaseStream.Position -= 4;
                }
                else if (section.OffsetMapIDCount > 0)
                {
                    // this will be the first m_sparseEntries entry
                    // confirm it's size is not zero otherwise it is invalid
                    completelyZero = reader.Read<SparseEntry>().Size == 0;
                    reader.BaseStream.Position -= 6;
                }
                else
                {
                    // there is no additional data and recordsData is already known to be zeroed
                    // therefore the record will have an id of zero which is invalid
                    completelyZero = true;
                }

                if (completelyZero)
                {
                    previousRecordCount += section.NumRecords;
                    continue;
                }
            }

            Array.Resize(ref recordsData, recordsData.Length + 8); // pad with extra zeros so we don't crash when reading

            // index data
            var indexData = reader.ReadArray<int>((uint)(section.IndexDataSize / 4));
            var isIndexEmpty = Header.HasIndexTable() && indexData.Count(i => i == 0) == section.NumRecords;

            // duplicate rows data
            Dictionary<int, int> copyData = new();

            for (var i = 0; i < section.CopyTableCount; i++)
                copyData[reader.ReadInt32()] = reader.ReadInt32();

            if (section.OffsetMapIDCount > 0)
                sparseEntries = reader.ReadArray<SparseEntry>((uint)section.OffsetMapIDCount);

            // reference data
            ReferenceData refData = null;

            if (section.ParentLookupDataSize > 0)
            {
                refData = new ReferenceData
                {
                    NumRecords = reader.ReadInt32(),
                    MinId = reader.ReadInt32(),
                    MaxId = reader.ReadInt32(),
                    Entries = new Dictionary<int, int>()
                };

                var entries = reader.ReadArray<ReferenceEntry>((uint)refData.NumRecords);
                foreach (var entry in entries)
                    refData.Entries[entry.Index] = entry.Id;
            }
            else
            {
                refData = new ReferenceData
                {
                    Entries = new Dictionary<int, int>()
                };
            }

            if (section.OffsetMapIDCount > 0)
            {
                var sparseIndexData = reader.ReadArray<int>((uint)section.OffsetMapIDCount);

                if (Header.HasIndexTable() && indexData.Length != sparseIndexData.Length)
                    throw new Exception("indexData.Length != sparseIndexData.Length");

                indexData = sparseIndexData;
            }

            BitReader bitReader = new(recordsData);

            for (var i = 0; i < section.NumRecords; ++i)
            {
                bitReader.Position = 0;
                if (Header.HasOffsetTable())
                    bitReader.Offset = sparseEntries[i].Offset - section.FileOffset;
                else
                    bitReader.Offset = i * (int)Header.RecordSize;

                var hasRef = refData.Entries.TryGetValue(i, out var refId);

                var recordIndex = i + previousRecordCount;
                var recordOffset = (recordIndex * Header.RecordSize) - (Header.RecordCount * Header.RecordSize);

                var rec = new WDC4Row(this, bitReader, (int)recordOffset, Header.HasIndexTable() ? (isIndexEmpty ? i : indexData[i]) : -1, hasRef ? refId : -1, stringsTable);
                Records.Add(rec.Id, rec);
            }

            foreach (var copyRow in copyData)
            {
                if (copyRow.Key == 0)
                    continue;

                var rec = Records[copyRow.Value].Clone();
                rec.Id = copyRow.Key;
                Records.Add(copyRow.Key, rec);
            }

            previousRecordCount += section.NumRecords;
        }

        return true;
    }
}