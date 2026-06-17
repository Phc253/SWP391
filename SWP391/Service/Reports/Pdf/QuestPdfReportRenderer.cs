using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SWP391.Models.Report;

namespace SWP391.Service.Reports.Pdf
{
    public class QuestPdfReportRenderer : IReportPdfRenderer
    {
        public byte[] RenderPapersReport(PaperReportResponse data, ReportFilters filters)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(24);
                    page.DefaultTextStyle(s => s.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Papers Report").FontSize(16).Bold();
                        col.Item().Text(BuildFilterLine(filters, data.TotalCount))
                            .FontSize(9).FontColor(Colors.Grey.Darken2);
                    });

                    page.Content().PaddingVertical(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(45);
                            c.RelativeColumn(4);
                            c.ConstantColumn(45);
                            c.ConstantColumn(50);
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                            c.RelativeColumn(3);
                        });

                        table.Header(h =>
                        {
                            HeaderCell(h.Cell(), "ID");
                            HeaderCell(h.Cell(), "Title");
                            HeaderCell(h.Cell(), "Year");
                            HeaderCell(h.Cell(), "Citations");
                            HeaderCell(h.Cell(), "Journal");
                            HeaderCell(h.Cell(), "Keywords");
                            HeaderCell(h.Cell(), "Authors");
                        });

                        foreach (var p in data.Items)
                        {
                            BodyCell(table.Cell(), p.PaperId.ToString());
                            BodyCell(table.Cell(), p.Title);
                            BodyCell(table.Cell(), p.PublicationYear?.ToString() ?? "");
                            BodyCell(table.Cell(), p.CitationCount?.ToString() ?? "");
                            BodyCell(table.Cell(), p.JournalName ?? "");
                            BodyCell(table.Cell(), string.Join("; ", p.Keywords));
                            BodyCell(table.Cell(), string.Join("; ", p.Authors));
                        }
                    });

                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span($"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC  -  Page ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        public byte[] RenderKeywordStatsReport(IReadOnlyList<KeywordStatReport> data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(s => s.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Keyword Statistics Report").FontSize(16).Bold();
                        col.Item().Text($"{data.Count} keywords, sorted by total papers")
                            .FontSize(9).FontColor(Colors.Grey.Darken2);
                    });

                    page.Content().PaddingVertical(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.ConstantColumn(80);
                            c.ConstantColumn(80);
                            c.ConstantColumn(80);
                        });

                        table.Header(h =>
                        {
                            HeaderCell(h.Cell(), "Keyword");
                            HeaderCell(h.Cell(), "Total papers");
                            HeaderCell(h.Cell(), "First year");
                            HeaderCell(h.Cell(), "Last year");
                        });

                        foreach (var k in data)
                        {
                            BodyCell(table.Cell(), k.KeywordText);
                            BodyCell(table.Cell(), k.TotalPapers.ToString());
                            BodyCell(table.Cell(), k.FirstYear?.ToString() ?? "");
                            BodyCell(table.Cell(), k.LastYear?.ToString() ?? "");
                        }
                    });

                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span($"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC  -  Page ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static string BuildFilterLine(ReportFilters filters, int totalCount)
        {
            var parts = new List<string> { $"{totalCount} rows" };
            if (filters.Year.HasValue) parts.Add($"year={filters.Year.Value}");
            if (!string.IsNullOrWhiteSpace(filters.KeywordText)) parts.Add($"keyword=\"{filters.KeywordText}\"");
            return string.Join("   ·   ", parts);
        }

        private static void HeaderCell(IContainer cell, string text) =>
            cell.Background(Colors.Grey.Lighten3).Padding(4).Text(text).SemiBold();

        private static void BodyCell(IContainer cell, string text) =>
            cell.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text);
    }
}
