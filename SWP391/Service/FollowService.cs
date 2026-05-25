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
                return ServiceResult<bool>.Fail("TargetType is required (e.g., 'Author', 'Journal', 'ResearchTopic').");
            }

            var type = request.TargetType.Trim();

            // [BƯỚC 1]: Kiểm tra thực thể có tồn tại không
            if (type.Equals("Author", StringComparison.OrdinalIgnoreCase))
            {
                var author = await _authorRepository.GetAuthorByIdAsync((int)request.TargetId);
                if (author == null)
                {
                    return ServiceResult<bool>.Fail("Author not found.");
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
                return ServiceResult<bool>.Fail($"TargetType '{type}' is not supported yet. Only 'Author', 'Journal', and 'ResearchTopic' are supported.");
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

            var authorIds = follows
                .Where(f => f.TargetType.Equals("Author", StringComparison.OrdinalIgnoreCase))
                .Select(f => (int)f.TargetId)
                .Distinct()
                .ToList();

            var journalIds = follows
                .Where(f => f.TargetType.Equals("Journal", StringComparison.OrdinalIgnoreCase))
                .Select(f => (int)f.TargetId)
                .Distinct()
                .ToList();

            var topicIds = follows
                .Where(f => f.TargetType.Equals("ResearchTopic", StringComparison.OrdinalIgnoreCase))
                .Select(f => (int)f.TargetId)
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

            var topics = new List<ResearchTopic>();
            if (topicIds.Any())
            {
                topics = await _dbContext.ResearchTopics.Where(t => topicIds.Contains(t.TopicId)).ToListAsync();
            }

            foreach (var f in follows)
            {
                var item = new FollowItemResponse
                {
                    FollowId = f.FollowId,
                    TargetId = f.TargetId,
                    TargetType = f.TargetType,
                    CreatedAt = f.CreatedAt
                };

                if (f.TargetType.Equals("Author", StringComparison.OrdinalIgnoreCase))
                {
                    var author = authors.FirstOrDefault(a => a.AuthorId == f.TargetId);
                    if (author != null)
                    {
                        item.AuthorName = author.AuthorName;
                        item.PaperCount = author.Papers?.Count ?? 0;
                    }
                }
                else if (f.TargetType.Equals("Journal", StringComparison.OrdinalIgnoreCase))
                {
                    var journal = journals.FirstOrDefault(j => j.JournalId == f.TargetId);
                    if (journal != null)
                    {
                        item.JournalName = journal.JournalName;
                    }
                }
                else if (f.TargetType.Equals("ResearchTopic", StringComparison.OrdinalIgnoreCase))
                {
                    var topic = topics.FirstOrDefault(t => t.TopicId == f.TargetId);
                    if (topic != null)
                    {
                        item.TopicName = topic.TopicName;
                    }
                }

                resultList.Add(item);
            }

            return ServiceResult<List<FollowItemResponse>>.Ok(resultList);
        }
    }
}
