using System.Collections.Generic;

namespace SWP391.Models.Author
{
    public class AuthorProfileResponse
    {
        public int AuthorId { get; set; }           // ID trong DB local
        public string AuthorName { get; set; } = null!;
        
        // Enrichment từ OpenAlex API
        public string? Affiliation { get; set; }     // "Distinguished Professor, Naval Postgraduate School"
        public int? WorksCount { get; set; }         // Tổng số papers (từ OpenAlex)
        public int? CitedByCount { get; set; }       // Tổng trích dẫn
        
        // Papers từ DB local
        public List<AuthorPaperItem> Papers { get; set; } = new();
        
        // Trạng thái follow (nếu user đã đăng nhập)
        public bool IsFollowed { get; set; }
    }

    public class AuthorPaperItem
    {
        public long PaperId { get; set; }
        public string Title { get; set; } = null!;
        public int? PublicationYear { get; set; }
        public string? JournalName { get; set; }
    }
}
