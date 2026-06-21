using PaniniTracker.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PaniniTracker.Core.Services
{
    public class CollectionService
    {
        public HashSet<string> AllStickers { get; private set; } = new();

        public HashSet<string> OwnedStickers { get; private set; } = new();

        public HashSet<string> IgnoredCodes { get; private set; } = new();

        public Dictionary<string, int> StickerPages { get; private set; } = new();
        public Dictionary<string, int> DuplicateStickers { get; private set; } = new();
        public void LoadData(SaveData data)
        {
            AllStickers = data.All.ToHashSet();
            OwnedStickers = data.Owned.ToHashSet();
            IgnoredCodes = data.Ignored.ToHashSet();
            StickerPages = data.Pages;
            DuplicateStickers = data.Duplicates ?? new();
        }
        public SaveData GetSaveData()
        {
            return new SaveData
            {
                All = AllStickers.ToList(),
                Owned = OwnedStickers.ToList(),
                Ignored = IgnoredCodes.ToList(),
                Pages = StickerPages,
                Duplicates = DuplicateStickers
            };
        }
        public void ImportAlbumList(string text)
        {
            var stickers = ParseAlbumList(text);

            AllStickers = stickers.Select(x => x.Code).ToHashSet();
            StickerPages = stickers.ToDictionary(x => x.Code, x => x.Page);

            OwnedStickers.RemoveWhere(code => !AllStickers.Contains(code));
        }
        public AddStickerResult AddOwnedCodes(string text)
        {
            return AddOwnedCodes(ParseCodes(text));
        }
        public AddStickerResult AddOwnedCodes(List<string> codes)
        {
            var result = new AddStickerResult();

            foreach (var code in codes)
            {
                if (AllStickers.Count > 0 && !AllStickers.Contains(code))
                {
                    IgnoredCodes.Add(code);
                    result.Unknown++;
                    continue;
                }

                if (OwnedStickers.Contains(code))
                {
                    if (!DuplicateStickers.ContainsKey(code))
                        DuplicateStickers[code] = 0;

                    DuplicateStickers[code]++;

                    result.Duplicates++;
                    continue;
                }

                OwnedStickers.Add(code);
                result.Added++;
            }

            return result;
        }
        public int RemoveOwnedCodes(string text)
        {
            return RemoveOwnedCodes(ParseCodes(text));
        }

        public int RemoveOwnedCodes(List<string> codes)
        {
            int removed = 0;

            foreach (var code in codes)
            {
                if (OwnedStickers.Remove(code))
                    removed++;
            }

            return removed;
        }
        public bool RemoveOwnedCode(string code)
        {
            return OwnedStickers.Remove(code.ToUpperInvariant().Trim());
        }
        public List<DuplicateStickerRow> GetDuplicateRows(string search, string group, string sortMode)
        {
            return DuplicateStickers
                .Where(x => x.Value > 0)
                .Where(x => MatchFilter(x.Key, search, group))
                .Select(x => new DuplicateStickerRow
                {
                    Code = x.Key,
                    Count = x.Value,
                    Page = StickerPages.TryGetValue(x.Key, out var page) ? page : null
                })
                .OrderBy(x => sortMode == "Σελίδα" ? x.Page ?? int.MaxValue : 0)
                .ThenBy(x => x.Code)
                .ToList();
        }

        public int GetTotalDuplicates()
        {
            return DuplicateStickers.Values.Sum();
        }
        public List<StickerRow> GetMissingRows(string search, string group, string sortMode)
        {
            return SortCodes(
                    AllStickers
                        .Except(OwnedStickers)
                        .Where(code => MatchFilter(code, search, group)),
                    sortMode)
                .Select(ToStickerRow)
                .ToList();
        }
        public List<StickerRow> GetOwnedRows(string search, string group, string sortMode)
        {
            return SortCodes(
                    OwnedStickers.Where(code => MatchFilter(code, search, group)),
                    sortMode)
                .Select(ToStickerRow)
                .ToList();
        }
        public List<StickerRow> GetIgnoredRows(string search, string group, string sortMode)
        {
            return SortCodes(
                    IgnoredCodes.Where(code => MatchFilter(code, search, group)),
                    sortMode)
                .Select(ToStickerRow)
                .ToList();
        }
        public CollectionSummary GetSummary()
        {
            int total = AllStickers.Count;
            int owned = OwnedStickers.Count;
            int missing = total - owned;

            return new CollectionSummary
            {
                Total = total,
                Owned = owned,
                Missing = missing,
                CompletionPercentage = total == 0
                    ? 0
                    : Math.Round(owned * 100.0 / total, 1)
            };
        }
        public List<GroupStatsRow> GetGroupStats()
        {
            return AllStickers
                .GroupBy(GetPrefix)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .Select(g =>
                {
                    var total = g.Count();
                    var owned = g.Count(code => OwnedStickers.Contains(code));
                    var missing = total - owned;

                    return new GroupStatsRow
                    {
                        Group = g.Key,
                        Owned = owned,
                        Total = total,
                        Missing = missing,
                        CompletionPercentage = total == 0
                            ? 0
                            : Math.Round(owned * 100.0 / total, 1)
                    };
                })
                .OrderByDescending(x => x.CompletionPercentage)
                .ThenBy(x => x.Missing)
                .ToList();
        }
        public List<GroupStatsRow> GetTop5ClosestGroups()
        {
            return GetGroupStats()
                .Where(x => x.Missing > 0)
                .OrderBy(x => x.Missing)
                .ThenByDescending(x => x.CompletionPercentage)
                .Take(5)
                .ToList();
        }
        public List<string> GetGroups()
        {
            var groups = AllStickers
                .Select(GetPrefix)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            groups.Insert(0, "ΟΛΑ");

            return groups;
        }
        public string ExportMissingText()
        {
            var lines = SortCodes(AllStickers.Except(OwnedStickers), "Σελίδα")
                .Select(code =>
                {
                    var page = StickerPages.TryGetValue(code, out var p)
                        ? $"{p} | "
                        : "";

                    return $"{page}{code}";
                });

            return string.Join(Environment.NewLine, lines);
        }
        public static List<string> ParseCodes(string text)
        {
            var result = new List<string>();

            var parts = Regex.Split(text.ToUpperInvariant(), @"[\s,;]+")
                .Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (var part in parts)
            {
                var range = Regex.Match(part, @"^([A-Z]+)(\d+)-([A-Z]*)(\d+)$");

                if (range.Success)
                {
                    var prefix1 = range.Groups[1].Value;
                    var start = int.Parse(range.Groups[2].Value);
                    var prefix2 = range.Groups[3].Value;
                    var end = int.Parse(range.Groups[4].Value);

                    var prefix = string.IsNullOrEmpty(prefix2)
                        ? prefix1
                        : prefix2;

                    for (int i = Math.Min(start, end); i <= Math.Max(start, end); i++)
                        result.Add($"{prefix}{i}");

                    continue;
                }

                result.Add(part.Trim());
            }

            return result.Distinct().ToList();
        }
        public static List<string> ParseDuplicateCodes(string text)
        {
            var result = new List<string>();

            var parts = Regex.Split(text.ToUpperInvariant(), @"[\s,;]+")
                .Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (var part in parts)
            {
                var range = Regex.Match(part, @"^([A-Z]+)(\d+)-([A-Z]*)(\d+)$");

                if (range.Success)
                {
                    var prefix1 = range.Groups[1].Value;
                    var start = int.Parse(range.Groups[2].Value);
                    var prefix2 = range.Groups[3].Value;
                    var end = int.Parse(range.Groups[4].Value);

                    var prefix = string.IsNullOrEmpty(prefix2)
                        ? prefix1
                        : prefix2;

                    for (int i = Math.Min(start, end); i <= Math.Max(start, end); i++)
                    {
                        result.Add($"{prefix}{i}");
                    }

                    continue;
                }

                result.Add(part.Trim());
            }

            return result;
        }
        public static List<StickerDefinition> ParseAlbumList(string text)
        {
            var result = new List<StickerDefinition>();

            foreach (var rawLine in text.Split(Environment.NewLine))
            {
                var line = rawLine.Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split('|');

                if (parts.Length != 2)
                    continue;

                if (!int.TryParse(parts[0].Trim(), out int page))
                    continue;

                var codePart = parts[1]
                    .Replace("&", " ")
                    .Replace(",", " ")
                    .Replace(";", " ")
                    .Trim();

                var tokens = Regex.Split(codePart, @"\s+")
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                foreach (var tokenRaw in tokens)
                {
                    var token = Regex.Replace(tokenRaw, @"\s+", "");

                    var range = Regex.Match(token, @"^([A-Z]+)(\d+)-([A-Z]*)(\d+)$");

                    if (range.Success)
                    {
                        var prefix1 = range.Groups[1].Value;
                        var start = int.Parse(range.Groups[2].Value);
                        var prefix2 = range.Groups[3].Value;
                        var end = int.Parse(range.Groups[4].Value);

                        var prefix = string.IsNullOrEmpty(prefix2)
                            ? prefix1
                            : prefix2;

                        for (int i = Math.Min(start, end); i <= Math.Max(start, end); i++)
                        {
                            result.Add(new StickerDefinition
                            {
                                Code = $"{prefix}{i}",
                                Page = page
                            });
                        }

                        continue;
                    }

                    if (Regex.IsMatch(token, @"^[A-Z]+$"))
                    {
                        for (int i = 1; i <= 20; i++)
                        {
                            result.Add(new StickerDefinition
                            {
                                Code = $"{token}{i}",
                                Page = page
                            });
                        }

                        continue;
                    }

                    result.Add(new StickerDefinition
                    {
                        Code = token,
                        Page = page
                    });
                }
            }

            return result
                .GroupBy(x => x.Code)
                .Select(x => x.First())
                .ToList();
        }
        private bool MatchFilter(string code, string search, string group)
        {
            search = search.Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(search) && !code.Contains(search))
                return false;

            if (group != "ΟΛΑ" && !code.StartsWith(group))
                return false;

            return true;
        }
        private StickerRow ToStickerRow(string code)
        {
            return new StickerRow
            {
                Code = code,
                Page = StickerPages.TryGetValue(code, out var page) ? page : null
            };
        }
        private IEnumerable<string> SortCodes(IEnumerable<string> codes, string sortMode)
        {
            if (sortMode == "Σελίδα")
            {
                return codes
                    .OrderBy(code => StickerPages.TryGetValue(code, out var page) ? page : int.MaxValue)
                    .ThenBy(NaturalSortKey);
            }

            return codes.OrderBy(NaturalSortKey);
        }
        private static string GetPrefix(string code)
        {
            var match = Regex.Match(code, @"^[A-Z]+");
            return match.Success ? match.Value : "";
        }
        private static string NaturalSortKey(string code)
        {
            return Regex.Replace(code, @"\d+", m => m.Value.PadLeft(6, '0'));
        }
    }
}
