using System;
using System.Collections.Generic;

namespace SWP391.Models.Bookmark
{
    public class BookmarkItemResponse
    {
        public long BookmarkId { get; set; }
        public long TargetId { get; set; }
        public string TargetType { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }

        // Thông tin hiển thị cho Bài báo (giống Google Scholar)
        public string? Title { get; set; }
        public string? Abstract { get; set; }
        public int? PublicationYear { get; set; }
        public string? JournalName { get; set; }
        public List<string> Authors { get; set; } = new List<string>();
    }
}
