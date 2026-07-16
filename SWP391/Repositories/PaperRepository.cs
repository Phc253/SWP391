using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models.Papers;

namespace SWP391.Repositories
{
    public class PaperRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public PaperRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<(List<Paper> Papers, int TotalCount)> SearchPapersAsync(
            string? keyword,
            string? author,
            string? journal,
            int page,
            int pageSize,
            int? publicationYear = null)
        {
            return SearchPapersAsync(new PaperSearchRequest
            {
                Keyword = keyword,
                Author = author,
                Journal = journal,
                Page = page,
                PageSize = pageSize,
                PublicationYear = publicationYear
            });
        }

        public async Task<(List<Paper> Papers, int TotalCount)> SearchPapersAsync(PaperSearchRequest filters)
        {
            var query = _dbContext.Papers
                .Include(p => p.Journal)
                .Include(p => p.Authors)
                .Include(p => p.Keywords)
                .AsQueryable();

            query = ApplyPaperFilters(query, filters);

            var totalCount = await query.CountAsync();

            var papers = await query
                .OrderByDescending(p => p.PublicationYear)
                .ThenBy(p => p.Title)
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return (papers, totalCount);
        }

        public async Task<PaperFacetResponse> GetAuthorFacetsAsync(string? q, int page, int pageSize)
        {
            var query = _dbContext.Authors
                .AsNoTracking()
                .Where(a => a.Papers.Any());

            q = Clean(q);
            if (q != null)
            {
                query = query.Where(a => a.AuthorName != null && a.AuthorName.Contains(q));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Select(a => new PaperFacetItemResponse
                {
                    Id = a.AuthorId.ToString(),
                    Name = a.AuthorName ?? string.Empty,
                    PaperCount = a.Papers.Count()
                })
                .OrderByDescending(a => a.PaperCount)
                .ThenBy(a => a.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaperFacetResponse { TotalCount = totalCount, Items = items };
        }

        public async Task<PaperFacetResponse> GetKeywordFacetsAsync(string? q, int page, int pageSize)
        {
            var query = _dbContext.Keywords
                .AsNoTracking()
                .Where(k => k.Papers.Any());

            q = Clean(q);
            if (q != null)
            {
                query = query.Where(k => k.KeywordText != null && k.KeywordText.Contains(q));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Select(k => new PaperFacetItemResponse
                {
                    Id = k.KeywordId.ToString(),
                    Name = k.KeywordText ?? string.Empty,
                    PaperCount = k.Papers.Count()
                })
                .OrderByDescending(k => k.PaperCount)
                .ThenBy(k => k.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaperFacetResponse { TotalCount = totalCount, Items = items };
        }

        public async Task<PaperFacetResponse> GetTopicFacetsAsync(string? q, int page, int pageSize)
        {
            var query = _dbContext.ResearchTopics
                .AsNoTracking()
                .Where(t => t.Keywords.Any(k => k.Papers.Any()));

            q = Clean(q);
            if (q != null)
            {
                query = query.Where(t => t.TopicName != null && t.TopicName.Contains(q));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Select(t => new PaperFacetItemResponse
                {
                    Id = t.TopicId.ToString(),
                    Name = t.TopicName ?? string.Empty,
                    PaperCount = t.Keywords
                        .SelectMany(k => k.Papers)
                        .Select(p => p.PaperId)
                        .Distinct()
                        .Count()
                })
                .OrderByDescending(t => t.PaperCount)
                .ThenBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaperFacetResponse { TotalCount = totalCount, Items = items };
        }

        public async Task<PaperFacetResponse> GetJournalFacetsAsync(string? q, int page, int pageSize)
        {
            var query = _dbContext.Journals
                .AsNoTracking()
                .Where(j => j.Papers.Any());

            q = Clean(q);
            if (q != null)
            {
                query = query.Where(j => j.JournalName != null && j.JournalName.Contains(q));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Select(j => new PaperFacetItemResponse
                {
                    Id = j.JournalId.ToString(),
                    Name = j.JournalName ?? string.Empty,
                    PaperCount = j.Papers.Count()
                })
                .OrderByDescending(j => j.PaperCount)
                .ThenBy(j => j.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaperFacetResponse { TotalCount = totalCount, Items = items };
        }

        public async Task<Paper?> GetPaperByIdAsync(long id)
        {
            return await _dbContext.Papers
                .Include(p => p.Journal)
                .Include(p => p.Authors)
                .Include(p => p.Keywords)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaperId == id);
        }

        public async Task<List<Paper>> GetPapersByIdsAsync(IEnumerable<long> ids)
        {
            return await _dbContext.Papers
                .Include(p => p.Journal)
                .Include(p => p.Authors)
                .Where(p => ids.Contains(p.PaperId))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(Author? Author, int WorkCount, int CitationCount)> GetAuthorStatsAsync(int authorId)
        {
            var author = await _dbContext.Authors
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AuthorId == authorId);

            if (author == null)
            {
                return (null, 0, 0);
            }

            var stats = await _dbContext.PaperAuthors
                .Where(pa => pa.AuthorId == authorId)
                .Select(pa => pa.Paper)
                .GroupBy(p => 1)
                .Select(g => new
                {
                    WorkCount = g.Count(),
                    CitationCount = g.Sum(p => p.CitationCount ?? 0)
                })
                .FirstOrDefaultAsync();

            return (author, stats?.WorkCount ?? 0, stats?.CitationCount ?? 0);
        }

        private static IQueryable<Paper> ApplyPaperFilters(IQueryable<Paper> query, PaperSearchRequest filters)
        {
            var q = Clean(filters.Q);
            if (q != null)
            {
                query = query.Where(p =>
                    (p.Title != null && p.Title.Contains(q)) ||
                    (p.Abstract != null && p.Abstract.Contains(q)) ||
                    p.Keywords.Any(k =>
                        (k.KeywordText != null && k.KeywordText.Contains(q)) ||
                        (k.Topic != null && k.Topic.TopicName != null && k.Topic.TopicName.Contains(q))) ||
                    p.Authors.Any(a => a.AuthorName != null && a.AuthorName.Contains(q)) ||
                    (p.Journal != null && p.Journal.JournalName != null && p.Journal.JournalName.Contains(q)));
            }

            var title = Clean(filters.Title);
            if (title != null)
            {
                query = query.Where(p => p.Title != null && p.Title.Contains(title));
            }

            var keyword = Clean(filters.Keyword);
            if (keyword != null)
            {
                query = query.Where(p => p.Keywords.Any(k => k.KeywordText != null && k.KeywordText.Contains(keyword)));
            }

            var topic = Clean(filters.Topic);
            if (topic != null)
            {
                query = query.Where(p => p.Keywords.Any(k =>
                    k.Topic != null &&
                    k.Topic.TopicName != null &&
                    k.Topic.TopicName.Contains(topic)));
            }

            var author = Clean(filters.Author);
            if (author != null)
            {
                query = query.Where(p => p.Authors.Any(a => a.AuthorName != null && a.AuthorName.Contains(author)));
            }

            var journal = Clean(filters.Journal);
            if (journal != null)
            {
                query = query.Where(p => p.Journal != null && p.Journal.JournalName != null && p.Journal.JournalName.Contains(journal));
            }

            if (filters.PublicationYear.HasValue)
            {
                query = query.Where(p => p.PublicationYear == filters.PublicationYear.Value);
            }

            if (HasValues(filters.AuthorIds))
            {
                query = query.Where(p => p.Authors.Any(a => filters.AuthorIds!.Contains(a.AuthorId)));
            }

            if (HasValues(filters.KeywordIds))
            {
                query = query.Where(p => p.Keywords.Any(k => filters.KeywordIds!.Contains(k.KeywordId)));
            }

            if (HasValues(filters.TopicIds))
            {
                query = query.Where(p => p.Keywords.Any(k => k.TopicId.HasValue && filters.TopicIds!.Contains(k.TopicId.Value)));
            }

            if (HasValues(filters.JournalIds))
            {
                query = query.Where(p => p.JournalId.HasValue && filters.JournalIds!.Contains(p.JournalId.Value));
            }

            return query;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool HasValues(List<int>? values)
        {
            return values != null && values.Count > 0;
        }

    }
}
