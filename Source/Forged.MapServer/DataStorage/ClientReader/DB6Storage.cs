// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Framework.Constants;
using Framework.Database;
using Framework.Dynamic;
using Framework.IO;
using Serilog;

namespace Forged.MapServer.DataStorage.ClientReader;

[Serializable]
public class DB6Storage<T> : Dictionary<uint, T>, IDB2Storage where T : new()
{
    private readonly Locale _defaultLocale;
    private readonly HotfixDatabase _hotfixDatabase;
    private readonly string _tableName = typeof(T).Name;
    private string _db2Name;
    private WDCHeader _header;
    public DB6Storage(HotfixDatabase hotfixDatabase, Locale defaultLocale = Locale.enUS)
    {
        _hotfixDatabase = hotfixDatabase;
        _defaultLocale = defaultLocale;
    }

    public void EraseRecord(uint id)
    {
        Remove(id);
    }

    public string GetName()
    {
        return string.IsNullOrEmpty(_db2Name) ? _tableName : _db2Name;
    }

    public uint GetNumRows()
    {
        return Keys.Max() + 1;
    }

    public uint GetTableHash()
    {
        return _header.TableHash;
    }

    public bool HasRecord(uint id)
    {
        return ContainsKey(id);
    }

    public void LoadData(string fullFileName, string db2Name)
    {
        if (!File.Exists(fullFileName))
        {
            Log.Logger.Error($"File {fullFileName} not found.");

            return;
        }

        _db2Name = db2Name;
        DBReader reader = new();

        using (var stream = new FileStream(fullFileName, FileMode.Open))
        {
            if (!reader.Load(stream))
            {
                Log.Logger.Error($"Error loading {fullFileName}.");

                return;
            }
        }

        _header = reader.Header;

        foreach (var b in reader.Records)
            Add((uint)b.Key, b.Value.As<T>());
    }

    public void LoadHotfixData(BitSet availableDb2Locales, HotfixStatements preparedStatement, HotfixStatements preparedStatementLocale)
    {
        LoadFromDB(false, preparedStatement);
        LoadFromDB(true, preparedStatement);

        if (preparedStatementLocale == 0)
            return;

        for (Locale locale = 0; locale < Locale.Total; ++locale)
        {
            if (!availableDb2Locales[(int)locale])
                continue;

            LoadStringsFromDB(false, locale, preparedStatementLocale);
            LoadStringsFromDB(true, locale, preparedStatementLocale);
        }
    }

    public void WriteRecord(uint id, Locale locale, ByteBuffer buffer)
    {
        var entry = this.LookupByKey(id);

        foreach (var fieldInfo in entry.GetType().GetFields())
        {
            if (fieldInfo.Name == "Id" && _header.HasIndexTable())
                continue;

            var type = fieldInfo.FieldType;

            if (type.IsArray)
            {
                WriteArrayValues(entry, fieldInfo, buffer);

                continue;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    buffer.WriteUInt8((byte)((bool)fieldInfo.GetValue(entry) ? 1 : 0));

                    break;
                case TypeCode.SByte:
                    buffer.WriteInt8((sbyte)fieldInfo.GetValue(entry));

                    break;
                case TypeCode.Byte:
                    buffer.WriteUInt8((byte)fieldInfo.GetValue(entry));

                    break;
                case TypeCode.Int16:
                    buffer.WriteInt16((short)fieldInfo.GetValue(entry));

                    break;
                case TypeCode.UInt16:
                    buffer.WriteUInt16((ushort)fieldInfo.GetValue(entry));

                    break;
                case TypeCode.Int32:
                    buffer.WriteInt32((int)fieldInfo.GetValue(entry));

                    break;
                case TypeCode.UInt32:
                    buffer.WriteUInt32((uint)fieldInfo.GetValue(entry));

                    break;
                case TypeCode.Int64:
                    buffer.WriteInt64((long)fieldInfo.GetValue(entry));

                    break;
                case TypeCode.UInt64:
                    buffer.WriteUInt64((ulong)fieldInfo.GetValue(entry));

                    break;
                case TypeCode.Single:
                    buffer.WriteFloat((float)fieldInfo.GetValue(entry));

                    break;
                case TypeCode.String:
                    buffer.WriteCString((string)fieldInfo.GetValue(entry));

                    break;
                case TypeCode.Object:
                    switch (type.Name)
                    {
                        case "LocalizedString":
                            var locStr = (LocalizedString)fieldInfo.GetValue(entry);

                            if (!locStr.HasString(locale))
                            {
                                locale = 0;

                                if (!locStr.HasString(locale))
                                {
                                    buffer.WriteUInt8(0);

                                    break;
                                }
                            }

                            var str = locStr[locale];
                            buffer.WriteCString(str);

                            break;
                        case "Vector2":
                            var vector2 = (Vector2)fieldInfo.GetValue(entry);
                            buffer.WriteVector2(vector2);

                            break;
                        case "Vector3":
                            var vector3 = (Vector3)fieldInfo.GetValue(entry);
                            buffer.WriteVector3(vector3);

                            break;
                        case "FlagArray128":
                            var flagArray128 = (FlagArray128)fieldInfo.GetValue(entry);
                            buffer.WriteUInt32(flagArray128[0]);
                            buffer.WriteUInt32(flagArray128[1]);
                            buffer.WriteUInt32(flagArray128[2]);
                            buffer.WriteUInt32(flagArray128[3]);

                            break;
                        default:
                            throw new Exception($"Unhandled Custom type: {type.Name}");
                    }

                    break;
            }
        }
    }
    private void LoadFromDB(bool custom, HotfixStatements preparedStatement)
    {
        // Even though this query is executed only once, prepared statement is used to send data from mysql server in binary format
        var stmt = _hotfixDatabase.GetPreparedStatement(preparedStatement);
        stmt.AddValue(0, !custom);
        var result = _hotfixDatabase.Query(stmt);

        if (result.IsEmpty())
            return;

        do
        {
            var obj = new T();

            var dbIndex = 0;
            var fields = typeof(T).GetFields();

            foreach (var f in fields)
            {
                var type = f.FieldType;

                if (type.IsArray)
                {
                    var arrayElementType = type.GetElementType();

                    if (arrayElementType.IsEnum)
                        arrayElementType = arrayElementType.GetEnumUnderlyingType();

                    var array = (Array)f.GetValue(obj);

                    switch (Type.GetTypeCode(arrayElementType))
                    {
                        case TypeCode.SByte:
                            f.SetValue(obj, ReadArray<sbyte>(result, dbIndex, array.Length));

                            break;
                        case TypeCode.Byte:
                            f.SetValue(obj, ReadArray<byte>(result, dbIndex, array.Length));

                            break;
                        case TypeCode.Int16:
                            f.SetValue(obj, ReadArray<short>(result, dbIndex, array.Length));

                            break;
                        case TypeCode.UInt16:
                            f.SetValue(obj, ReadArray<ushort>(result, dbIndex, array.Length));

                            break;
                        case TypeCode.Int32:
                            f.SetValue(obj, ReadArray<int>(result, dbIndex, array.Length));

                            break;
                        case TypeCode.UInt32:
                            f.SetValue(obj, ReadArray<uint>(result, dbIndex, array.Length));

                            break;
                        case TypeCode.Int64:
                            f.SetValue(obj, ReadArray<long>(result, dbIndex, array.Length));

                            break;
                        case TypeCode.UInt64:
                            f.SetValue(obj, ReadArray<ulong>(result, dbIndex, array.Length));

                            break;
                        case TypeCode.Single:
                            f.SetValue(obj, ReadArray<float>(result, dbIndex, array.Length));

                            break;
                        case TypeCode.String:
                            f.SetValue(obj, ReadArray<string>(result, dbIndex, array.Length));

                            break;
                        case TypeCode.Object:
                            if (arrayElementType == typeof(Vector3))
                            {
                                var values = ReadArray<float>(result, dbIndex, array.Length * 3);

                                var vectors = new Vector3[array.Length];

                                for (var i = 0; i < array.Length; ++i)
                                    vectors[i] = new Vector3(values[(i * 3)..(3 + (i * 3))]);

                                f.SetValue(obj, vectors);

                                dbIndex += array.Length * 3;
                            }

                            continue;
                        default:
                            Log.Logger.Error("Wrong Array Type: {0}", arrayElementType.Name);

                            break;
                    }

                    dbIndex += array.Length;
                }
                else
                {
                    if (type.IsEnum)
                        type = type.GetEnumUnderlyingType();

                    switch (Type.GetTypeCode(type))
                    {
                        case TypeCode.SByte:
                            f.SetValue(obj, result.Read<sbyte>(dbIndex++));

                            break;
                        case TypeCode.Byte:
                            f.SetValue(obj, result.Read<byte>(dbIndex++));

                            break;
                        case TypeCode.Int16:
                            f.SetValue(obj, result.Read<short>(dbIndex++));

                            break;
                        case TypeCode.UInt16:
                            f.SetValue(obj, result.Read<ushort>(dbIndex++));

                            break;
                        case TypeCode.Int32:
                            f.SetValue(obj, result.Read<int>(dbIndex++));

                            break;
                        case TypeCode.UInt32:
                            f.SetValue(obj, result.Read<uint>(dbIndex++));

                            break;
                        case TypeCode.Int64:
                            f.SetValue(obj, result.Read<long>(dbIndex++));

                            break;
                        case TypeCode.UInt64:
                            f.SetValue(obj, result.Read<ulong>(dbIndex++));

                            break;
                        case TypeCode.Single:
                            f.SetValue(obj, result.Read<float>(dbIndex++));

                            break;
                        case TypeCode.String:
                            var str = result.Read<string>(dbIndex++);
                            f.SetValue(obj, str);

                            break;
                        case TypeCode.Object:
                            if (type == typeof(LocalizedString))
                            {
                                LocalizedString locString = new()
                                {
                                    [_defaultLocale] = result.Read<string>(dbIndex++)
                                };

                                f.SetValue(obj, locString);
                            }
                            else if (type == typeof(Vector2))
                            {
                                f.SetValue(obj, new Vector2(ReadArray<float>(result, dbIndex, 2)));
                                dbIndex += 2;
                            }
                            else if (type == typeof(Vector3))
                            {
                                f.SetValue(obj, new Vector3(ReadArray<float>(result, dbIndex, 3)));
                                dbIndex += 3;
                            }
                            else if (type == typeof(FlagArray128))
                            {
                                f.SetValue(obj, new FlagArray128(ReadArray<uint>(result, dbIndex, 4)));
                                dbIndex += 4;
                            }

                            break;
                        default:
                            Log.Logger.Error("Wrong Type: {0}", type.Name);

                            break;
                    }
                }
            }

            if (fields.Length != 0)
            {
                var id = (uint)fields[_header.IdIndex == -1 ? 0 : _header.IdIndex].GetValue(obj);
                base[id] = obj;
            }
        } while (result.NextRow());
    }

    private void LoadStringsFromDB(bool custom, Locale locale, HotfixStatements preparedStatement)
    {
        var stmt = _hotfixDatabase.GetPreparedStatement(preparedStatement);
        stmt.AddValue(0, !custom);
        stmt.AddValue(1, locale.ToString());
        var result = _hotfixDatabase.Query(stmt);

        if (result.IsEmpty())
            return;

        do
        {
            var index = 0;
            if (!this.TryGetValue(result.Read<uint>(index++), out var obj))
                continue;

            foreach (var f in typeof(T).GetFields())
            {
                if (f.FieldType != typeof(LocalizedString))
                    continue;

                var locString = (LocalizedString)f.GetValue(obj);
                locString[locale] = result.Read<string>(index++);
            }
        } while (result.NextRow());
    }

    private TValue[] ReadArray<TValue>(SQLResult result, int dbIndex, int arrayLength)
    {
        var values = new TValue[arrayLength];

        for (var i = 0; i < arrayLength; ++i)
            values[i] = result.Read<TValue>(dbIndex + i);

        return values;
    }

    private void WriteArrayValues(object entry, FieldInfo fieldInfo, ByteBuffer buffer)
    {
        var type = fieldInfo.FieldType.GetElementType();
        var array = (Array)fieldInfo.GetValue(entry);

        for (var i = 0; i < array.Length; ++i)
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    buffer.WriteUInt8((byte)((bool)array.GetValue(i) ? 1 : 0));

                    break;
                case TypeCode.SByte:
                    buffer.WriteInt8((sbyte)array.GetValue(i));

                    break;
                case TypeCode.Byte:
                    buffer.WriteUInt8((byte)array.GetValue(i));

                    break;
                case TypeCode.Int16:
                    buffer.WriteInt16((short)array.GetValue(i));

                    break;
                case TypeCode.UInt16:
                    buffer.WriteUInt16((ushort)array.GetValue(i));

                    break;
                case TypeCode.Int32:
                    buffer.WriteInt32((int)array.GetValue(i));

                    break;
                case TypeCode.UInt32:
                    buffer.WriteUInt32((uint)array.GetValue(i));

                    break;
                case TypeCode.Int64:
                    buffer.WriteInt64((long)array.GetValue(i));

                    break;
                case TypeCode.UInt64:
                    buffer.WriteUInt64((ulong)array.GetValue(i));

                    break;
                case TypeCode.Single:
                    buffer.WriteFloat((float)array.GetValue(i));

                    break;
                case TypeCode.String:
                    var str = (string)array.GetValue(i);
                    buffer.WriteCString(str);

                    break;
            }
    }
}