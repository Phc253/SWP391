using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Bookmark;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class BookmarkService
    {
        private readonly BookmarkRepository _bookmarkRepository;
        private readonly PaperRepository _paperRepository;
        private readonly TrendRepository _trendRepository;

        public BookmarkService(BookmarkRepository bookmarkRepository, PaperRepository paperRepository, TrendRepository trendRepository)
        {
            _bookmarkRepository = bookmarkRepository;
            _paperRepository = paperRepository;
            _trendRepository = trendRepository;
        }

        public async Task<ServiceResult<bool>> ToggleBookmarkAsync(int userId, ToggleBookmarkRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TargetType))
            {
                return ServiceResult<bool>.Fail("TargetType is required (e.g., 'Paper' or 'Keyword').");
            }

            var type = request.TargetType.Trim();

            // Chỉ cho phép "Paper" hoặc "Keyword"
            if (!type.Equals("Paper", StringComparison.OrdinalIgnoreCase) && !type.Equals("Keyword", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<bool>.Fail("Invalid TargetType. Supported types are 'Paper' and 'Keyword'.");
            }

            // [BƯỚC 1]: Kiểm tra xem đối tượng mà người dùng muốn lưu có thật sự tồn tại trong hệ thống hay không (Validate Target Exists)
            if (type.Equals("Paper", StringComparison.OrdinalIgnoreCase))
            {
                var paper = await _paperRepository.GetPaperByIdAsync(request.TargetId);
                if (paper == null)
                {
                    return ServiceResult<bool>.Fail("Paper not found.");
                }
            }
            else if (type.Equals("Keyword", StringComparison.OrdinalIgnoreCase))
            {
                // Ép kiểu xuồng int vì KeywordId là int
                int keywordId = (int)request.TargetId;
                var keyword = await _trendRepository.GetKeywordByIdAsync(keywordId);
                if (keyword == null)
                {
                    return ServiceResult<bool>.Fail("Keyword not found.");
                }
            }

            // [BƯỚC 2]: Kiểm tra trong CSDL xem record bookmark (của User + Target) này đã tồn tại chưa
            var existingBookmark = await _bookmarkRepository.GetBookmarkAsync(userId, request.TargetId, type);
            if (existingBookmark != null)
            {
                // Đã bookmark => Thực hiện bỏ bookmark (Un-bookmark)
                await _bookmarkRepository.RemoveBookmarkAsync(existingBookmark);
                return ServiceResult<bool>.Ok(false); 
            }
            else
            {
                // Chưa bookmark => Thực hiện bookmark mới
                var newBookmark = new Bookmark
                {
                    UserId = userId,
                    TargetId = request.TargetId,
                    TargetType = type,
                    CreatedAt = DateTime.UtcNow
                };
                await _bookmarkRepository.AddBookmarkAsync(newBookmark);
                return ServiceResult<bool>.Ok(true);
            }
        }

        public async Task<ServiceResult<List<BookmarkItemResponse>>> GetUserBookmarksAsync(int userId)
        {
            var bookmarks = await _bookmarkRepository.GetUserBookmarksAsync(userId);
            var resultList = new List<BookmarkItemResponse>();

            // Tách các Type ra để query dữ liệu gốc (tránh N+1)
            var validBookmarks = bookmarks
                .Where(b => b.TargetId.HasValue && !string.IsNullOrWhiteSpace(b.TargetType))
                .ToList();

            var paperIds = validBookmarks.Where(b => b.TargetType!.Equals("Paper", StringComparison.OrdinalIgnoreCase))
                                         .Select(b => b.TargetId!.Value)
                                         .ToList();

            var keywordIds = validBookmarks.Where(b => b.TargetType!.Equals("Keyword", StringComparison.OrdinalIgnoreCase))
                                           .Select(b => (int)b.TargetId!.Value)
                                           .ToList();

            var papers = new List<Paper>();
            if (paperIds.Any())
            {
                papers = await _paperRepository.GetPapersByIdsAsync(paperIds);
            }

            var keywords = new List<Keyword>();
            if (keywordIds.Any())
            {
                keywords = await _trendRepository.GetKeywordsByIdsAsync(keywordIds);
            }

            foreach (var b in validBookmarks)
            {
                var targetId = b.TargetId!.Value;
                var targetType = b.TargetType!;
                var item = new BookmarkItemResponse
                {
                    BookmarkId = b.BookmarkId,
                    TargetId = targetId,
                    TargetType = targetType,
                    CreatedAt = b.CreatedAt
                };

                if (targetType.Equals("Paper", StringComparison.OrdinalIgnoreCase))
                {
                    var paper = papers.FirstOrDefault(p => p.PaperId == targetId);
                    if (paper != null)
                    {
                        item.Title = paper.Title;
                        item.Abstract = paper.Abstract;
                        item.PublicationYear = paper.PublicationYear;
                        item.CitationCount = paper.CitationCount;
                        item.JournalName = paper.Journal?.JournalName;
                        item.Authors = paper.Authors
                            .Where(a => !string.IsNullOrWhiteSpace(a.AuthorName))
                            .Select(a => a.AuthorName!)
                            .ToList();
                    }
                }
                else if (targetType.Equals("Keyword", StringComparison.OrdinalIgnoreCase))
                {
                    var keyword = keywords.FirstOrDefault(k => k.KeywordId == (int)targetId);
                    if (keyword != null)
                    {
                        item.KeywordText = keyword.KeywordText;
                    }
                }
                
                resultList.Add(item);
            }

            return ServiceResult<List<BookmarkItemResponse>>.Ok(resultList);
        }
    }
}
