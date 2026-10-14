namespace LibrarySystem.Services
{
    /// <summary>
    /// Minimal CSV writer - no third-party package needed for something this
    /// small. RFC 4180-style escaping: a field containing a comma, a quote or a
    /// newline is wrapped in quotes, with any quote inside it doubled. Lines are
    /// joined with CRLF, which is both the CSV standard and what Excel expects
    /// natively.
    /// </summary>
    public class CsvWriter
    {
        private readonly List<string> _lines = [];

        /// <summary>Appends a single line verbatim, not escaped - for a section label or a filter line, not field data.</summary>
        public CsvWriter AddLine(string line)
        {
            _lines.Add(line);
            return this;
        }

        /// <summary>Appends a blank line - used as a separator between sections.</summary>
        public CsvWriter AddBlankLine() => AddLine(string.Empty);

        /// <summary>Appends one CSV row, escaping each field as needed.</summary>
        public CsvWriter AddRow(params string?[] fields)
        {
            _lines.Add(string.Join(",", fields.Select(EscapeField)));
            return this;
        }

        public string Build() => string.Join("\r\n", _lines) + "\r\n";

        /// <summary>Quotes a field if it contains a comma, a quote or a newline, doubling any quote inside it. Null becomes an empty field.</summary>
        public static string EscapeField(string? value)
        {
            value ??= string.Empty;
            if (value.IndexOfAny([',', '"', '\n', '\r']) < 0)
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
