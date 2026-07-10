using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Follow;
using SWP391.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SWP391.Service
{
    public class FollowService
    {
        private readonly FollowRepository _followRepository;
        private readonly AuthorRepository _authorRepository;
        private readonly ScientificTrendDbContext _dbContext;

        public FollowService(FollowRepository followRepository, AuthorRepository authorRepository, ScientificTrendDbContext dbContext)
        {
            _followRepository = followRepository;
            _authorRepository = authorRepository;
            _dbContext = dbContext;
        }

        public async Task<ServiceResult<bool>> ToggleFollowAsync(int userId, ToggleFollowRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TargetType))
            {
                return ServiceResult<bool>.Fail("TargetType is required (e.g., 'Author', 'Keyword', 'Journal', 'ResearchTopic').");
            }

            var type = NormalizeTargetType(request.TargetType);

            // [BƯỚC 1]: Kiểm tra thực thể có tồn tại không
            if (type.Equals("Author", StringComparison.OrdinalIgnoreCase))
            {
                var author = await _authorRepository.GetAuthorByIdAsync((int)request.TargetId);
                if (author == null)
                {
                    return ServiceResult<bool>.Fail("Author not found.");
                }
            }
            else if (type.Equals("Keyword", StringComparison.OrdinalIgnoreCase))
            {
                var keyword = await _dbContext.Keywords.FindAsync((int)request.TargetId);
                if (keyword == null)
                {
                    return ServiceResult<bool>.Fail("Keyword not found.");
                }
            }
            else if (type.Equals("Journal", StringComparison.OrdinalIgnoreCase))
            {
                var journal = await _dbContext.Journals.FindAsync((int)request.TargetId);
                if (journal == null)
                {
                    return ServiceResult<bool>.Fail("Journal not found.");
                }
            }
            else if (type.Equals("ResearchTopic", StringComparison.OrdinalIgnoreCase))
            {
                var topic = await _dbContext.ResearchTopics.FindAsync((int)request.TargetId);
                if (topic == null)
                {
                    return ServiceResult<bool>.Fail("ResearchTopic not found.");
                }
            }
            else
            {
                return ServiceResult<bool>.Fail($"TargetType '{type}' is not supported yet. Only 'Author', 'Keyword', 'Journal', and 'ResearchTopic' are supported.");
            }

            // [BƯỚC 2]: Toggle follow
            var existingFollow = await _followRepository.GetFollowAsync(userId, request.TargetId, type);
            if (existingFollow != null)
            {
                await _followRepository.RemoveFollowAsync(existingFollow);
                return ServiceResult<bool>.Ok(false); // Unfollowed
            }
            else
            {
                var newFollow = new Follow
                {
                    UserId = userId,
                    TargetId = request.TargetId,
                    TargetType = type,
                    CreatedAt = DateTime.UtcNow
                };
                await _followRepository.AddFollowAsync(newFollow);
                return ServiceResult<bool>.Ok(true); // Followed
            }
        }

        public async Task<ServiceResult<List<FollowItemResponse>>> GetUserFollowsAsync(int userId)
        {
            var follows = await _followRepository.GetUserFollowsAsync(userId);
            var resultList = new List<FollowItemResponse>();

            var validFollows = follows
                .Where(f => f.TargetId.HasValue && !string.IsNullOrWhiteSpace(f.TargetType))
                .ToList();

            var authorIds = validFollows
                .Where(f => f.TargetType!.Equals("Author", StringComparison.OrdinalIgnoreCase))
                .Select(f => (int)f.TargetId!.Value)
                .Distinct()
                .ToList();

            var journalIds = validFollows
                .Where(f => f.TargetType!.Equals("Journal", StringComparison.OrdinalIgnoreCase))
                .Select(f => (int)f.TargetId!.Value)
                .Distinct()
                .ToList();

            var keywordIds = validFollows
                .Where(f => f.TargetType!.Equals("Keyword", StringComparison.OrdinalIgnoreCase))
                .Select(f => (int)f.TargetId!.Value)
                .Distinct()
                .ToList();

            var topicIds = validFollows
                .Where(f => f.TargetType!.Equals("ResearchTopic", StringComparison.OrdinalIgnoreCase))
                .Select(f => (int)f.TargetId!.Value)
                .Distinct()
                .ToList();

            var authors = new List<Author>();
            if (authorIds.Any())
            {
                authors = await _authorRepository.GetAuthorsByIdsAsync(authorIds);
            }

            var journals = new List<Journal>();
            if (journalIds.Any())
            {
                journals = await _dbContext.Journals.Where(j => journalIds.Contains(j.JournalId)).ToListAsync();
            }

            var keywords = new List<Keyword>();
            if (keywordIds.Any())
            {
                keywords = await _dbContext.Keywords
                    .Include(k => k.Papers)
                    .Where(k => keywordIds.Contains(k.KeywordId))
                    .ToListAsync();
            }

            var topics = new List<ResearchTopic>();
            if (topicIds.Any())
            {
                topics = await _dbContext.ResearchTopics.Where(t => topicIds.Contains(t.TopicId)).ToListAsync();
            }

            foreach (var f in validFollows)
            {
                var targetId = f.TargetId!.Value;
                var targetType = f.TargetType!;
                var item = new FollowItemResponse
                {
                    FollowId = f.FollowId,
                    TargetId = targetId,
                    TargetType = targetType,
                    CreatedAt = f.CreatedAt
                };

                if (targetType.Equals("Author", StringComparison.OrdinalIgnoreCase))
                {
                    var author = authors.FirstOrDefault(a => a.AuthorId == targetId);
                    if (author != null)
                    {
                        item.AuthorName = author.AuthorName;
                        item.PaperCount = author.Papers?.Count ?? 0;
                    }
                }
                else if (targetType.Equals("Journal", StringComparison.OrdinalIgnoreCase))
                {
                    var journal = journals.FirstOrDefault(j => j.JournalId == targetId);
                    if (journal != null)
                    {
                        item.JournalName = journal.JournalName;
                    }
                }
                else if (targetType.Equals("Keyword", StringComparison.OrdinalIgnoreCase))
                {
                    var keyword = keywords.FirstOrDefault(k => k.KeywordId == targetId);
                    if (keyword != null)
                    {
                        item.KeywordText = keyword.KeywordText;
                        item.PaperCount = keyword.Papers?.Count ?? 0;
                    }
                }
                else if (targetType.Equals("ResearchTopic", StringComparison.OrdinalIgnoreCase))
                {
                    var topic = topics.FirstOrDefault(t => t.TopicId == targetId);
                    if (topic != null)
                    {
                        item.TopicName = topic.TopicName;
                    }
                }

                resultList.Add(item);
            }

            return ServiceResult<List<FollowItemResponse>>.Ok(resultList);
        }

        private static string NormalizeTargetType(string targetType)
        {
            var type = targetType.Trim();
            if (type.Equals("Topic", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("ResearchTopic", StringComparison.OrdinalIgnoreCase))
            {
                return "ResearchTopic";
            }

            if (type.Equals("Journal", StringComparison.OrdinalIgnoreCase))
            {
                return "Journal";
            }

            if (type.Equals("Keyword", StringComparison.OrdinalIgnoreCase))
            {
                return "Keyword";
            }

            if (type.Equals("Author", StringComparison.OrdinalIgnoreCase))
            {
                return "Author";
            }

            return type;
        }
    }
}
