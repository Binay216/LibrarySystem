using LibrarySystem.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibrarySystem.Services.Pdf
{
    /// <summary>
    /// Shared page chrome for every report PDF: header (report name, branch,
    /// date range, generated timestamp), footer (page numbers), the app's
    /// burgundy accent colour, and reusable summary/table composition helpers.
    /// Tables repeat their header row across page breaks (QuestPDF's
    /// table.Header() does this natively - it's the reason to use it over
    /// plain rows) and fall back to "No data for this period" when empty -
    /// same wording as the CSV export, but without an empty table underneath:
    /// a table with only a header row reads as a rendering glitch in a visual
    /// document, which isn't a concern the CSV has.
    ///
    /// A shared base with report-specific ComposeContent overrides, not one
    /// fully generic document class: the three reports' tables differ enough
    /// in column shape that a single configurable renderer would end up more
    /// complex than three small, explicit derived classes sharing this common
    /// layout - "a shared layout with report-specific content".
    /// </summary>
    public abstract class ReportPdfDocumentBase(string reportTitle, ReportFilterInfo filters, DateTime generatedAt) : IDocument
    {
        protected const string AccentColor = "#7b2d42";

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text(reportTitle).FontSize(18).Bold().FontColor(AccentColor);

                column.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text($"Branch: {filters.BranchName}");
                    row.RelativeItem().Text($"Period: {filters.StartDate:yyyy-MM-dd} to {filters.EndDate:yyyy-MM-dd}");
                    row.RelativeItem().AlignRight().Text($"Generated: {generatedAt:yyyy-MM-dd HH:mm}");
                });

                column.Item().PaddingTop(6).BorderBottom(1).BorderColor(AccentColor);
            });
        }

        private static void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(text =>
            {
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        }

        protected abstract void ComposeContent(IContainer container);

        /// <summary>
        /// A band of headline figures (e.g. Total Loans / Active / Overdue),
        /// visually distinct (shaded background, bold accent-coloured values)
        /// from the data tables below it.
        /// </summary>
        protected static void ComposeSummary(ColumnDescriptor column, params (string Label, string Value)[] figures)
        {
            column.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
            {
                foreach (var (label, value) in figures)
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().Text(value).FontSize(14).Bold().FontColor(AccentColor);
                    });
                }
            });
        }

        /// <summary>
        /// A section heading, then a table with the given column headers
        /// (repeated across page breaks) and one row per item via
        /// <paramref name="addRow"/> - or "No data for this period" if
        /// <paramref name="items"/> is empty.
        /// </summary>
        protected static void ComposeTable<T>(
            ColumnDescriptor column,
            string sectionTitle,
            string[] headers,
            IReadOnlyList<T> items,
            Action<TableDescriptor, T> addRow)
        {
            column.Item().PaddingTop(12).Text(sectionTitle).FontSize(12).Bold().FontColor(AccentColor);

            if (items.Count == 0)
            {
                column.Item().PaddingTop(2).Text("No data for this period.").Italic().FontColor(Colors.Grey.Darken2);
                return;
            }

            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in headers)
                    {
                        columns.RelativeColumn();
                    }
                });

                table.Header(header =>
                {
                    foreach (var h in headers)
                    {
                        header.Cell().Background(AccentColor).Padding(5).Text(h).FontColor(Colors.White).Bold();
                    }
                });

                foreach (var item in items)
                {
                    addRow(table, item);
                }
            });
        }
    }
}
