using System.Text;

namespace KenketsuAvailability.Services;

/// <summary>
/// RFC 4180 相当のCSVパーサ。Googleスプレッドシートの出力を読むためだけに使う。
/// 引用符で囲まれたセル内の改行・カンマ・二重引用符に対応する。
/// </summary>
public static class Csv
{
    public static List<string[]> Parse(string text)
    {
        List<string[]> rows = [];
        List<string> row = [];
        StringBuilder cell = new();
        bool inQuotes = false;
        //空文字列と「最後の行の後ろ」を区別するため、セルを1つでも読んだかを覚えておく
        bool started = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    //"" はエスケープされた引用符
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    cell.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    started = true;
                    break;

                case ',':
                    row.Add(cell.ToString());
                    cell.Clear();
                    started = true;
                    break;

                case '\r':
                    //CRLF の CR は読み飛ばす
                    break;

                case '\n':
                    row.Add(cell.ToString());
                    cell.Clear();
                    rows.Add([.. row]);
                    row.Clear();
                    started = false;
                    break;

                default:
                    cell.Append(c);
                    started = true;
                    break;
            }
        }

        if (started || cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add([.. row]);
        }

        return rows;
    }

    /// <summary>
    /// 1行目をヘッダとみなして、列名 → 値の辞書の一覧にする。列名の大小文字は無視する。
    /// </summary>
    public static List<Dictionary<string, string>> ParseWithHeader(string text)
    {
        var rows = Parse(text);
        if (rows.Count == 0) return [];

        var header = rows[0].Select(h => h.Trim()).ToArray();
        List<Dictionary<string, string>> result = [];

        foreach (var row in rows.Skip(1))
        {
            //完全に空の行は読み飛ばす（スプレッドシートの末尾に付きやすい）
            if (row.All(string.IsNullOrWhiteSpace)) continue;

            Dictionary<string, string> item = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Length; i++)
            {
                if (header[i].Length == 0) continue;
                item[header[i]] = i < row.Length ? row[i].Trim() : "";
            }
            result.Add(item);
        }
        return result;
    }
}
