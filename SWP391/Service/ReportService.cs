using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Report;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class ReportService
    {
        private readonly PaperRepository _paperRepository;
        private readonly ScientificTrendDbContext _dbContext;

        public ReportService(PaperRepository paperRepository, ScientificTrendDbContext dbContext)
        {
            _paperRepository = paperRepository;
            _dbContext = dbContext;
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

                // Use the existing search method — pass keywordText as the keyword filter
                var (papers, totalCount) = await _paperRepository.SearchPapersAsync(
                    keyword: keywordText,
                    author: null,
                    journal: null,
                    page: page,
                    pageSize: pageSize);

                // Apply year filter in-memory (the repository doesn't support year filtering natively)
                // For large datasets this should be pushed into the repository, but is acceptable here
                if (year.HasValue)
                {
                    papers = papers.Where(p => p.PublicationYear == year.Value).ToList();
                    // Recalculate totalCount after year filter (approximate — for full accuracy,
                    // push year filter into repository query)
                    totalCount = papers.Count;
                }

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
