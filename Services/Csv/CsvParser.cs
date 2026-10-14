using System.Text;

namespace LibrarySystem.Services.Csv
{
    /// <summary>
    /// A small RFC4180-style CSV reader: quoted fields, embedded commas and
    /// embedded newlines inside quotes, doubled-quote escaping ("" -> "), and
    /// CRLF/LF/CR line endings. Each field is trimmed of surrounding whitespace.
    ///
    /// This is a new reader, not a reuse of ReportCsvWriter/CsvWriter from the
    /// export work: those only ever WRITE values the app already controls (an
    /// escape-on-the-way-out problem - wrap a field in quotes if it contains a
    /// comma/quote/newline). Import has the inverse, harder problem - TOKENIZING
    /// untrusted, arbitrarily-formatted text back into fields, which needs a
    /// real character-by-character state machine (tracking whether we're inside
    /// quotes) rather than a line-by-line Split(','). A writer's escaping logic
    /// doesn't give you that for free.
    /// </summary>
    public static class CsvParser
    {
        public static List<string[]> Parse(string csvText)
        {
            var rows = new List<string[]>();
            var fields = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;
            var i = 0;
            var n = csvText.Length;

            void EndField()
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }

            void EndRow()
            {
                EndField();
                if (fields.Count > 1 || fields[0].Length > 0)
                {
                    rows.Add([.. fields]);
                }
                fields = [];
            }

            while (i < n)
            {
                var c = csvText[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < n && csvText[i + 1] == '"')
                        {
                            current.Append('"');
                            i += 2;
                            continue;
                        }

                        inQuotes = false;
                        i++;
                        continue;
                    }

                    current.Append(c);
                    i++;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        i++;
                        break;
                    case ',':
                        EndField();
                        i++;
                        break;
                    case '\r':
                        if (i + 1 < n && csvText[i + 1] == '\n')
                        {
                            i++;
                        }

                        EndRow();
                        i++;
                        break;
                    case '\n':
                        EndRow();
                        i++;
                        break;
                    default:
                        current.Append(c);
                        i++;
                        break;
                }
            }

            if (current.Length > 0 || fields.Count > 0)
            {
                EndRow();
            }

            return rows;
        }
    }
}
