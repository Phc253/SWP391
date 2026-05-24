using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Follow;
using SWP391.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP391.Service
{
    public class FollowService
    {
        private readonly FollowRepository _followRepository;
        private readonly AuthorRepository _authorRepository;

        public FollowService(FollowRepository followRepository, AuthorRepository authorRepository)
        {
            _followRepository = followRepository;
            _authorRepository = authorRepository;
        }

        public async Task<ServiceResult<bool>> ToggleFollowAsync(int userId, ToggleFollowRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TargetType))
            {
                return ServiceResult<bool>.Fail("TargetType is required (e.g., 'Author').");
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
            else
            {
                return ServiceResult<bool>.Fail($"TargetType '{type}' is not supported yet. Only 'Author' is supported.");
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

            var authors = new List<Author>();
            if (authorIds.Any())
            {
                authors = await _authorRepository.GetAuthorsByIdsAsync(authorIds);
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
                        item.PaperCount = author.Papers.Count;
                    }
                }

                resultList.Add(item);
            }

            return ServiceResult<List<FollowItemResponse>>.Ok(resultList);
        }
    }
}
