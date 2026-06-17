using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Report;
using SWP391.Repositories;
using SWP391.Service.Reports.Pdf;

namespace SWP391.Service
{
    public class ReportService
    {
        private readonly PaperRepository _paperRepository;
        private readonly ScientificTrendDbContext _dbContext;
        private readonly IReportPdfRenderer _pdfRenderer;

        public ReportService(PaperRepository paperRepository, ScientificTrendDbContext dbContext, IReportPdfRenderer pdfRenderer)
        {
            _paperRepository = paperRepository;
            _dbContext = dbContext;
            _pdfRenderer = pdfRenderer;
        }

        // Renders all matching papers (no pagination) to a PDF byte array via QuestPDF.
        public async Task<ServiceResult<byte[]>> ExportPapersReportPdfAsync(int? year, string? keywordText)
        {
            try
            {
                var (papers, _) = await _paperRepository.SearchPapersAsync(
                    keyword: keywordText, author: null, journal: null,
                    page: 1, pageSize: int.MaxValue, publicationYear: year);

                var data = new PaperReportResponse
                {
                    Page = 1,
                    PageSize = papers.Count,
                    TotalCount = papers.Count,
                    Items = papers.Select(p => new PaperReportItem
                    {
                        PaperId         = p.PaperId,
                        Title           = p.Title,
                        PublicationYear = p.PublicationYear,
                        CitationCount   = p.CitationCount,
                        JournalName     = p.Journal?.JournalName,
                        Keywords        = p.Keywords.Select(k => k.KeywordText).ToList(),
                        Authors         = p.Authors.Select(a => a.AuthorName).ToList()
                    }).ToList()
                };

                var pdf = _pdfRenderer.RenderPapersReport(data, new ReportFilters(year, keywordText));
                return ServiceResult<byte[]>.Ok(pdf);
            }
            catch (Exception ex)
            {
                return ServiceResult<byte[]>.Fail("PDF export failed: " + ex.Message);
            }
        }

        public async Task<ServiceResult<byte[]>> ExportKeywordStatsPdfAsync()
        {
            try
            {
                var stats = await GetKeywordStatsAsync();
                if (!stats.Success) return ServiceResult<byte[]>.Fail(stats.Error!);
                var pdf = _pdfRenderer.RenderKeywordStatsReport(stats.Data!);
                return ServiceResult<byte[]>.Ok(pdf);
            }
            catch (Exception ex)
            {
                return ServiceResult<byte[]>.Fail("PDF export failed: " + ex.Message);
            }
        }

        // Paginated paper report with optional year and keyword filters.
        // Reuses PaperRepository.SearchPapersAsync (which already handles keyword/journal/author
        // filtering with eager loading) and adds a year filter on top.
        public async Task<ServiceResult<PaperReportResponse>> GetPapersReportAsync(
            int page, int pageSize, int? year, string? keywordText)
        {
            try
            {
                // Clamp pageSize to prevent accidental oversized responses
                pageSize = Math.Clamp(pageSize, 1, 100);
                page = Math.Max(1, page);

                // Year filter is now applied at the repository level so totalCount and pagination stay correct.
                var (papers, totalCount) = await _paperRepository.SearchPapersAsync(
                    keyword: keywordText,
                    author: null,
                    journal: null,
                    page: page,
                    pageSize: pageSize,
                    publicationYear: year);

                var items = papers.Select(p => new PaperReportItem
                {
                    PaperId         = p.PaperId,
                    Title           = p.Title,
                    PublicationYear = p.PublicationYear,
                    CitationCount   = p.CitationCount,
                    JournalName     = p.Journal?.JournalName,
                    Keywords        = p.Keywords.Select(k => k.KeywordText).ToList(),
                    Authors         = p.Authors.Select(a => a.AuthorName).ToList()
                }).ToList();

                return ServiceResult<PaperReportResponse>.Ok(new PaperReportResponse
                {
                    Page       = page,
                    PageSize   = pageSize,
                    TotalCount = totalCount,
                    Items      = items
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<PaperReportResponse>.Fail(
                    "An error occurred while generating the paper report: " + ex.Message);
            }
        }

        // Fetches ALL matching papers (no pagination) and serializes to CSV.
        public async Task<ServiceResult<string>> ExportPapersReportAsync(int? year, string? keywordText)
        {
            try
            {
                var (papers, _) = await _paperRepository.SearchPapersAsync(
                    keyword: keywordText, author: null, journal: null,
                    page: 1, pageSize: int.MaxValue, publicationYear: year);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("PaperId,Title,PublicationYear,CitationCount,JournalName,Keywords,Authors");
                foreach (var p in papers)
                {
                    sb.AppendLine(string.Join(",",
                        p.PaperId,
                        EscapeCsv(p.Title),
                        p.PublicationYear?.ToString() ?? "",
                        p.CitationCount?.ToString() ?? "",
                        EscapeCsv(p.Journal?.JournalName ?? ""),
                        EscapeCsv(string.Join(";", p.Keywords.Select(k => k.KeywordText ?? ""))),
                        EscapeCsv(string.Join(";", p.Authors.Select(a => a.AuthorName ?? "")))));
                }
                return ServiceResult<string>.Ok(sb.ToString());
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Fail("Export failed: " + ex.Message);
            }
        }

        // Serializes all keyword stats to CSV.
        public async Task<ServiceResult<string>> ExportKeywordStatsAsync()
        {
            try
            {
                var statsResult = await GetKeywordStatsAsync();
                if (!statsResult.Success)
                    return ServiceResult<string>.Fail(statsResult.Error!);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("KeywordText,TotalPapers,FirstYear,LastYear");
                foreach (var k in statsResult.Data!)
                {
                    sb.AppendLine(string.Join(",",
                        EscapeCsv(k.KeywordText),
                        k.TotalPapers,
                        k.FirstYear?.ToString() ?? "",
                        k.LastYear?.ToString() ?? ""));
                }
                return ServiceResult<string>.Ok(sb.ToString());
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Fail("Export failed: " + ex.Message);
            }
        }

        // RFC 4180: wrap in double-quotes when value contains comma, quote, or newline.
        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        // Returns all keywords with their total paper count and year span.
        public async Task<ServiceResult<List<KeywordStatReport>>> GetKeywordStatsAsync()
        {
            try
            {
                var stats = await _dbContext.Keywords
                    .Where(k => k.Papers.Any())
                    .Select(k => new KeywordStatReport
                    {
                        KeywordText = k.KeywordText,
                        TotalPapers = k.Papers.Count(),
                        FirstYear   = k.Papers
                                        .Where(p => p.PublicationYear.HasValue)
                                        .Min(p => (int?)p.PublicationYear),
                        LastYear    = k.Papers
                                        .Where(p => p.PublicationYear.HasValue)
                                        .Max(p => (int?)p.PublicationYear)
                    })
                    .OrderByDescending(k => k.TotalPapers)
                    .ToListAsync();

                return ServiceResult<List<KeywordStatReport>>.Ok(stats);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<KeywordStatReport>>.Fail(
                    "An error occurred while generating keyword stats: " + ex.Message);
            }
        }
    }
}
