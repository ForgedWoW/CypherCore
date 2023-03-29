// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Globals;
using Forged.MapServer.Spells;
using Framework.Collections;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Chat;

public class LanguageManager
{
    private static readonly uint[] SHashtable =
    {
        0x486E26EE, 0xDCAA16B3, 0xE1918EEF, 0x202DAFDB, 0x341C7DC7, 0x1C365303, 0x40EF2D37, 0x65FD5E49, 0xD6057177, 0x904ECE93, 0x1C38024F, 0x98FD323B, 0xE3061AE7, 0xA39B0FA1, 0x9797F25F, 0xE4444563,
    };

    private readonly CliDB _cliDB;
    private readonly SpellManager _spellManager;

    private readonly MultiMap<uint, LanguageDesc> _langsMap = new();
    private readonly MultiMap<Tuple<uint, byte>, string> _wordsMap = new();

    public LanguageManager(CliDB cliDB, SpellManager spellManager)
    {
        _cliDB = cliDB;
        _spellManager = spellManager;
    }

    public void LoadSpellEffectLanguage(SpellEffectRecord spellEffect)
    {
        var languageId = (uint)spellEffect.EffectMiscValue[0];
        _langsMap.Add(languageId, new LanguageDesc(spellEffect.SpellID, 0)); // register without a skill id for now
    }

    public void LoadLanguages()
    {
        var oldMSTime = Time.MSTime;

        // Load languages from Languages.db2. Just the id, we don't need the name
        foreach (var langEntry in _cliDB.LanguagesStorage.Values)
        {
            var spellsRange = _langsMap.LookupByKey(langEntry.Id);

            if (spellsRange.Empty())
            {
                _langsMap.Add(langEntry.Id, new LanguageDesc());
            }
            else
            {
                List<LanguageDesc> langsWithSkill = new();

                foreach (var spellItr in spellsRange)
                    foreach (var skillPair in _spellManager.GetSkillLineAbilityMapBounds(spellItr.SpellId))
                        langsWithSkill.Add(new LanguageDesc(spellItr.SpellId, skillPair.SkillLine));

                foreach (var langDesc in langsWithSkill)
                {
                    // erase temporary assignment that lacked skill
                    _langsMap.Remove(langEntry.Id, new LanguageDesc(langDesc.SpellId, 0));
                    _langsMap.Add(langEntry.Id, langDesc);
                }
            }
        }

        // Add the languages used in code in case they don't exist
        _langsMap.Add((uint)Language.Universal, new LanguageDesc());
        _langsMap.Add((uint)Language.Addon, new LanguageDesc());
        _langsMap.Add((uint)Language.AddonLogged, new LanguageDesc());

        // Log load time
        Log.Logger.Information($"Loaded {_langsMap.Count} languages in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadLanguagesWords()
    {
        var oldMSTime = Time.MSTime;

        uint wordsNum = 0;

        foreach (var wordEntry in _cliDB.LanguageWordsStorage.Values)
        {
            var length = (byte)Math.Min(18, wordEntry.Word.Length);

            var key = Tuple.Create(wordEntry.LanguageID, length);

            _wordsMap.Add(key, wordEntry.Word);
            ++wordsNum;
        }

        // log load time
        Log.Logger.Information($"Loaded {_wordsMap.Count} word groups from {wordsNum} words in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public string Translate(string msg, uint language, Locale locale)
    {
        var textToTranslate = StripHyperlinks(msg);
        ReplaceUntranslatableCharactersWithSpace(ref textToTranslate);

        var result = "";
        StringArray tokens = new(textToTranslate, ' ');

        foreach (string str in tokens)
        {
            var wordLen = (uint)Math.Min(18, str.Length);
            var wordGroup = FindWordGroup(language, wordLen);

            if (!wordGroup.Empty())
            {
                var wordHash = SStrHash(str, true);
                var idxInsideGroup = (byte)(wordHash % wordGroup.Count());

                var replacementWord = wordGroup[idxInsideGroup];

                switch (locale)
                {
                    case Locale.koKR:
                    case Locale.zhCN:
                    case Locale.zhTW:
                    {
                        var length = Math.Min(str.Length, replacementWord.Length);

                        for (var i = 0; i < length; ++i)
                            if (str[i] >= 'A' && str[i] <= 'Z')
                                result += char.ToUpper(replacementWord[i]);
                            else
                                result += replacementWord[i];

                        break;
                    }
                    default:
                    {
                        var length = Math.Min(str.Length, replacementWord.Length);

                        for (var i = 0; i < length; ++i)
                            if (char.IsUpper(str[i]))
                                result += char.ToUpper(replacementWord[i]);
                            else
                                result += char.ToLower(replacementWord[i]);

                        break;
                    }
                }
            }

            result += ' ';
        }

        if (!result.IsEmpty())
            result.Remove(result.Length - 1);

        return result;
    }

    public bool IsLanguageExist(Language languageId)
    {
        return _cliDB.LanguagesStorage.HasRecord((uint)languageId);
    }

    public List<LanguageDesc> GetLanguageDescById(Language languageId)
    {
        return _langsMap.LookupByKey((uint)languageId);
    }

    public bool ForEachLanguage(Func<uint, LanguageDesc, bool> callback)
    {
        foreach (var pair in _langsMap.KeyValueList)
            if (!callback(pair.Key, pair.Value))
                return false;

        return true;
    }

    private List<string> FindWordGroup(uint language, uint wordLen)
    {
        return _wordsMap.LookupByKey(Tuple.Create(language, (byte)wordLen));
    }

    private string StripHyperlinks(string source)
    {
        var destChar = new char[source.Length];

        var destSize = 0;
        var skipSquareBrackets = false;

        for (var i = 0; i < source.Length; ++i)
        {
            var c = source[i];

            if (c != '|')
            {
                if (!skipSquareBrackets || (c != '[' && c != ']'))
                    destChar[destSize++] = source[i];

                continue;
            }

            if (i + 1 >= source.Length)
                break;

            switch (source[i + 1])
            {
                case 'c':
                case 'C':
                    // skip color
                    i += 9;

                    break;
                case 'r':
                    ++i;

                    break;
                case 'H':
                    // skip just past first |h
                    i = source.IndexOf("|h", i, StringComparison.Ordinal);

                    if (i != -1)
                        i += 2;

                    skipSquareBrackets = true;

                    break;
                case 'h':
                    ++i;
                    skipSquareBrackets = false;

                    break;
                case 'T':
                    // skip just past closing |t
                    i = source.IndexOf("|t", i, StringComparison.Ordinal);

                    if (i != -1)
                        i += 2;

                    break;
            }
        }

        return new string(destChar, 0, destSize);
    }

    private void ReplaceUntranslatableCharactersWithSpace(ref string text)
    {
        var chars = text.ToCharArray();

        for (var i = 0; i < text.Length; ++i)
        {
            var w = chars[i];

            if (!Extensions.isExtendedLatinCharacter(w) && !char.IsNumber(w) && w <= 0xFF && w != '\\')
                chars[i] = ' ';
        }

        text = new string(chars);
    }

    private static char upper_backslash(char c)
    {
        return c == '/' ? '\\' : char.ToUpper(c);
    }

    private uint SStrHash(string str, bool caseInsensitive, uint seed = 0x7FED7FED)
    {
        var shift = 0xEEEEEEEE;

        for (var i = 0; i < str.Length; ++i)
        {
            var c = str[i];

            if (caseInsensitive)
                c = upper_backslash(c);

            seed = (SHashtable[c >> 4] - SHashtable[c & 0xF]) ^ (shift + seed);
            shift = c + seed + 33 * shift + 3;
        }

        return seed != 0 ? seed : 1;
    }
}