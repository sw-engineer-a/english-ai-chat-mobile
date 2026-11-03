# Builds one small pack per age zip for the Unity app.
# Source: ../english-ai-chat-web/csv/english/*.zip
# Output: Assets/StreamingAssets/english/*.pack

$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$srcDir = Join-Path (Split-Path $repo -Parent) "english-ai-chat-web\csv\english"
$outDir = Join-Path $repo "Assets\StreamingAssets\english"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$code = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

public static class EnglishPacker
{
    const int Max = 40;

    class Row
    {
        public string Q;
        public string[] R;
        public int[] T;
    }

    public static string Pack(string zipPath, string outPath)
    {
        var folder = Path.GetFileNameWithoutExtension(zipPath);
        using (var zip = ZipFile.OpenRead(zipPath))
        {
            var names = ReadTopics(zip, folder);
            var buckets = new Dictionary<int, List<Row>>();
            foreach (var id in names.Keys) buckets[id] = new List<Row>();
            var seen = new HashSet<string>();
            var open = names.Count;
            var read = 0;
            for (var part = 1; part <= 10 && open > 0; part++)
            {
                var entry = zip.GetEntry(folder + "/part-" + part.ToString("00") + ".csv");
                if (entry == null) continue;
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                {
                    reader.ReadLine();
                    string raw;
                    while (open > 0 && (raw = reader.ReadLine()) != null)
                    {
                        if (raw.Length == 0) continue;
                        var cells = Parse(raw);
                        if (cells.Count < 9 || string.IsNullOrWhiteSpace(cells[0])) continue;
                        if (!seen.Add(cells[0])) continue;
                        int t1, t2, t3;
                        if (!int.TryParse(cells[6], out t1)) t1 = 0;
                        if (!int.TryParse(cells[7], out t2)) t2 = 0;
                        if (!int.TryParse(cells[8], out t3)) t3 = 0;
                        var row = new Row
                        {
                            Q = cells[0],
                            R = new[] { cells[1], cells[2], cells[3], cells[4], cells[5] },
                            T = new[] { t1, t2, t3 }
                        };
                        read++;
                        var topics = row.T;
                        for (var i = 0; i < topics.Length; i++)
                        {
                            List<Row> list;
                            if (!buckets.TryGetValue(topics[i], out list)) continue;
                            if (list.Count >= Max || list.Contains(row)) continue;
                            list.Add(row);
                            if (list.Count == Max) open--;
                        }
                    }
                }
            }

            var lines = new List<Row>();
            var used = new HashSet<Row>();
            var ids = new List<int>(names.Keys);
            ids.Sort();
            foreach (var id in ids)
            {
                foreach (var row in buckets[id])
                {
                    if (used.Add(row)) lines.Add(row);
                }
            }

            var pool = new List<string>();
            var index = new Dictionary<string, int>();
            foreach (var row in lines)
            {
                IndexOf(pool, index, row.Q);
                for (var i = 0; i < row.R.Length; i++) IndexOf(pool, index, row.R[i] ?? "");
            }

            using (var stream = File.Create(outPath))
            using (var writer = new BinaryWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(Encoding.ASCII.GetBytes("LDEN"));
                writer.Write((ushort)1);
                writer.Write((ushort)ids.Count);
                foreach (var id in ids)
                {
                    writer.Write((ushort)id);
                    var bytes = Encoding.UTF8.GetBytes(names[id] ?? "");
                    writer.Write((ushort)bytes.Length);
                    if (bytes.Length > 0) writer.Write(bytes);
                }
                writer.Write((ushort)pool.Count);
                foreach (var text in pool)
                {
                    var bytes = Encoding.UTF8.GetBytes(text);
                    writer.Write((ushort)bytes.Length);
                    if (bytes.Length > 0) writer.Write(bytes);
                }
                writer.Write((ushort)lines.Count);
                foreach (var row in lines)
                {
                    writer.Write((ushort)IndexOf(pool, index, row.Q));
                    for (var i = 0; i < row.R.Length; i++) writer.Write((ushort)IndexOf(pool, index, row.R[i] ?? ""));
                    for (var i = 0; i < row.T.Length; i++) writer.Write((ushort)row.T[i]);
                }
            }

            var size = new FileInfo(outPath).Length;
            return folder + ": " + lines.Count + " lines, " + pool.Count + " strings, " + size.ToString("N0") + " bytes, topics left " + open + ", rows read " + read.ToString("N0");
        }
    }

    static int IndexOf(List<string> pool, Dictionary<string, int> index, string text)
    {
        int found;
        if (index.TryGetValue(text, out found)) return found;
        found = pool.Count;
        pool.Add(text);
        index[text] = found;
        return found;
    }

    static Dictionary<int, string> ReadTopics(ZipArchive zip, string folder)
    {
        var names = new Dictionary<int, string>();
        var entry = zip.GetEntry(folder + "/topics.csv");
        using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
        {
            reader.ReadLine();
            string raw;
            while ((raw = reader.ReadLine()) != null)
            {
                var cells = Parse(raw);
                int id;
                if (cells.Count < 2 || !int.TryParse(cells[0], out id) || id <= 0) continue;
                names[id] = cells[1];
            }
        }
        return names;
    }

    static List<string> Parse(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (quoted)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else quoted = false;
                }
                else current.Append(ch);
                continue;
            }
            if (ch == '"') { quoted = true; continue; }
            if (ch == ',')
            {
                cells.Add(current.ToString());
                current.Length = 0;
                continue;
            }
            current.Append(ch);
        }
        cells.Add(current.ToString());
        return cells;
    }
}
'@

Add-Type -TypeDefinition $code -ReferencedAssemblies @("System.IO.Compression", "System.IO.Compression.FileSystem")
Get-ChildItem (Join-Path $srcDir "*.zip") | ForEach-Object {
    $out = Join-Path $outDir ($_.BaseName + ".pack")
    [EnglishPacker]::Pack($_.FullName, $out)
}
