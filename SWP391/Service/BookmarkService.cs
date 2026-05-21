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

        public BookmarkService(BookmarkRepository bookmarkRepository, PaperRepository paperRepository)
        {
            _bookmarkRepository = bookmarkRepository;
            _paperRepository = paperRepository;
        }

        public async Task<ServiceResult<bool>> ToggleBookmarkAsync(int userId, ToggleBookmarkRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TargetType))
            {
                return ServiceResult<bool>.Fail("TargetType is required (e.g., 'Paper').");
            }

            var type = request.TargetType.Trim();

            // [BƯỚC 1]: Kiểm tra xem bài báo (hoặc đối tượng khác) mà người dùng muốn lưu có thật sự tồn tại trong hệ thống hay không (Validate Target Exists)
            if (type.Equals("Paper", StringComparison.OrdinalIgnoreCase))
            {
                var paper = await _paperRepository.GetPaperByIdAsync(request.TargetId);
                if (paper == null)
                {
                    return ServiceResult<bool>.Fail("Paper not found.");
                }
            }
            // (Bạn có thể mở rộng logic kiểm tra "Keyword", "Journal" tại đây nếu cần)

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

        public async Task<ServiceResult<List<Bookmark>>> GetUserBookmarksAsync(int userId)
        {
            var bookmarks = await _bookmarkRepository.GetUserBookmarksAsync(userId);
            return ServiceResult<List<Bookmark>>.Ok(bookmarks);
        }
    }
}