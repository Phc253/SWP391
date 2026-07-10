using SWP391.Models;
using SWP391.Models.Researcher;
using SWP391.Repositories;

namespace SWP391.Service;

public class ResearcherService
{
    private static readonly HashSet<string> SupportedWatchTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Keyword",
        "Journal",
        "ResearchTopic",
        "Topic"
    };

    private readonly ResearcherRepository _researcherRepository;

    public ResearcherService(ResearcherRepository researcherRepository)
    {
        _researcherRepository = researcherRepository;
    }

    public async Task<ServiceResult<ResearcherDashboardResponse>> GetDashboardAsync(int years, int topN)
    {
        try
        {
            var validation = ValidateWindow(years, topN);
            if (validation != null)
            {
                return ServiceResult<ResearcherDashboardResponse>.Fail(validation);
            }

            var data = await _researcherRepository.GetDashboardAsync(years, topN);
            return ServiceResult<ResearcherDashboardResponse>.Ok(data);
        }
        catch (Exception ex)
        {
            return ServiceResult<ResearcherDashboardResponse>.Fail(
                "An error occurred while loading researcher dashboard: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<ResearcherRankedItemResponse>>> GetEmergingTopicsAsync(int years, int topN)
    {
        try
        {
            var validation = ValidateWindow(years, topN);
            if (validation != null)
            {
                return ServiceResult<List<ResearcherRankedItemResponse>>.Fail(validation);
            }

            var data = await _researcherRepository.GetEmergingTopicsAsync(years, topN);
            return ServiceResult<List<ResearcherRankedItemResponse>>.Ok(data);
        }
        catch (Exception ex)
        {
            return ServiceResult<List<ResearcherRankedItemResponse>>.Fail(
                "An error occurred while loading emerging topics: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<ResearcherRelatedTopicResponse>>> GetRelatedTopicsAsync(
        int? topicId,
        string? keywordText,
        int topN)
    {
        try
        {
            if (!topicId.HasValue && string.IsNullOrWhiteSpace(keywordText))
            {
                return ServiceResult<List<ResearcherRelatedTopicResponse>>.Fail(
                    "Either topicId or keywordText is required.");
            }

            if (topN < 1 || topN > 50)
            {
                return ServiceResult<List<ResearcherRelatedTopicResponse>>.Fail("topN must be between 1 and 50.");
            }

            var data = topicId.HasValue
                ? await _researcherRepository.GetRelatedTopicsByTopicAsync(topicId.Value, topN)
                : await _researcherRepository.GetRelatedTopicsByKeywordAsync(keywordText!.Trim(), topN);

            return ServiceResult<List<ResearcherRelatedTopicResponse>>.Ok(data);
        }
        catch (Exception ex)
        {
            return ServiceResult<List<ResearcherRelatedTopicResponse>>.Fail(
                "An error occurred while loading related topics: " + ex.Message);
        }
    }

    public async Task<ServiceResult<ResearcherComparisonResponse>> CompareKeywordsAsync(
        string left,
        string right,
        int years)
    {
        try
        {
            var validation = ValidateComparisonInput(left, right, years);
            if (validation != null)
            {
                return ServiceResult<ResearcherComparisonResponse>.Fail(validation);
            }

            var leftId = await _researcherRepository.ResolveKeywordIdAsync(left);
            var rightId = await _researcherRepository.ResolveKeywordIdAsync(right);
            if (!leftId.HasValue || !rightId.HasValue)
            {
                return ServiceResult<ResearcherComparisonResponse>.Fail("One or more keywords were not found.");
            }

            var items = await _researcherRepository.CompareKeywordsAsync(new[] { leftId.Value, rightId.Value }, years);
            return ServiceResult<ResearcherComparisonResponse>.Ok(new ResearcherComparisonResponse
            {
                TargetType = "Keyword",
                Years = years,
                Items = items
            });
        }
        catch (Exception ex)
        {
            return ServiceResult<ResearcherComparisonResponse>.Fail(
                "An error occurred while comparing keywords: " + ex.Message);
        }
    }

    public async Task<ServiceResult<ResearcherComparisonResponse>> CompareTopicsAsync(
        string left,
        string right,
        int years)
    {
        try
        {
            var validation = ValidateComparisonInput(left, right, years);
            if (validation != null)
            {
                return ServiceResult<ResearcherComparisonResponse>.Fail(validation);
            }

            var leftId = await _researcherRepository.ResolveTopicIdAsync(left);
            var rightId = await _researcherRepository.ResolveTopicIdAsync(right);
            if (!leftId.HasValue || !rightId.HasValue)
            {
                return ServiceResult<ResearcherComparisonResponse>.Fail("One or more topics were not found.");
            }

            var items = await _researcherRepository.CompareTopicsAsync(new[] { leftId.Value, rightId.Value }, years);
            return ServiceResult<ResearcherComparisonResponse>.Ok(new ResearcherComparisonResponse
            {
                TargetType = "ResearchTopic",
                Years = years,
                Items = items
            });
        }
        catch (Exception ex)
        {
            return ServiceResult<ResearcherComparisonResponse>.Fail(
                "An error occurred while comparing topics: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<ResearcherWatchlistItemResponse>>> GetWatchlistAsync(int userId)
    {
        try
        {
            var data = await _researcherRepository.GetWatchlistAsync(userId);
            return ServiceResult<List<ResearcherWatchlistItemResponse>>.Ok(data);
        }
        catch (Exception ex)
        {
            return ServiceResult<List<ResearcherWatchlistItemResponse>>.Fail(
                "An error occurred while loading researcher watchlist: " + ex.Message);
        }
    }

    public async Task<ServiceResult<ResearcherWatchlistItemResponse>> AddToWatchlistAsync(
        int userId,
        ResearcherWatchlistRequest request)
    {
        try
        {
            var targetType = NormalizeTargetType(request.TargetType);
            if (!SupportedWatchTypes.Contains(targetType))
            {
                return ServiceResult<ResearcherWatchlistItemResponse>.Fail(
                    "TargetType must be Keyword, Journal, or ResearchTopic.");
            }

            var exists = await _researcherRepository.TargetExistsAsync(request.TargetId, targetType);
            if (!exists)
            {
                return ServiceResult<ResearcherWatchlistItemResponse>.Fail("Watch target was not found.");
            }

            var existing = await _researcherRepository.GetFollowAsync(userId, request.TargetId, targetType);
            if (existing == null)
            {
                await _researcherRepository.AddFollowAsync(userId, request.TargetId, targetType);
            }

            var watchlist = await _researcherRepository.GetWatchlistAsync(userId);
            var item = watchlist.FirstOrDefault(w =>
                w.TargetId == request.TargetId &&
                w.TargetType.Equals(NormalizeTargetType(targetType), StringComparison.OrdinalIgnoreCase));

            return item == null
                ? ServiceResult<ResearcherWatchlistItemResponse>.Fail("Watch target could not be loaded after saving.")
                : ServiceResult<ResearcherWatchlistItemResponse>.Ok(item);
        }
        catch (Exception ex)
        {
            return ServiceResult<ResearcherWatchlistItemResponse>.Fail(
                "An error occurred while saving researcher watchlist: " + ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> RemoveFromWatchlistAsync(int userId, long targetId, string targetType)
    {
        try
        {
            var normalizedType = NormalizeTargetType(targetType);
            if (!SupportedWatchTypes.Contains(normalizedType))
            {
                return ServiceResult<bool>.Fail("TargetType must be Keyword, Journal, or ResearchTopic.");
            }

            var existing = await _researcherRepository.GetFollowAsync(userId, targetId, normalizedType);
            if (existing == null)
            {
                return ServiceResult<bool>.Fail("Watch target was not found.");
            }

            await _researcherRepository.RemoveFollowAsync(existing);
            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Fail(
                "An error occurred while removing researcher watchlist item: " + ex.Message);
        }
    }

    public async Task<ServiceResult<ResearcherReportResponse>> GetPublicationTrendReportAsync(
        string targetType,
        string target,
        int years)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetType) || string.IsNullOrWhiteSpace(target))
            {
                return ServiceResult<ResearcherReportResponse>.Fail("targetType and target are required.");
            }

            if (years < 1 || years > 50)
            {
                return ServiceResult<ResearcherReportResponse>.Fail("years must be between 1 and 50.");
            }

            var normalizedType = NormalizeTargetType(targetType);
            if (normalizedType == "Keyword")
            {
                var keywordId = await _researcherRepository.ResolveKeywordIdAsync(target);
                if (!keywordId.HasValue)
                {
                    return ServiceResult<ResearcherReportResponse>.Fail("Keyword was not found.");
                }

                var data = await _researcherRepository.CompareKeywordsAsync(new[] { keywordId.Value }, years);
                return BuildReport("PublicationTrend", $"Publication trend for keyword {data[0].Name}", data[0]);
            }

            if (normalizedType == "ResearchTopic")
            {
                var topicId = await _researcherRepository.ResolveTopicIdAsync(target);
                if (!topicId.HasValue)
                {
                    return ServiceResult<ResearcherReportResponse>.Fail("Topic was not found.");
                }

                var data = await _researcherRepository.CompareTopicsAsync(new[] { topicId.Value }, years);
                return BuildReport("PublicationTrend", $"Publication trend for topic {data[0].Name}", data[0]);
            }

            return ServiceResult<ResearcherReportResponse>.Fail("targetType must be Keyword or ResearchTopic.");
        }
        catch (Exception ex)
        {
            return ServiceResult<ResearcherReportResponse>.Fail(
                "An error occurred while generating publication trend report: " + ex.Message);
        }
    }

    public async Task<ServiceResult<ResearcherReportResponse>> GetKeywordComparisonReportAsync(
        string left,
        string right,
        int years)
    {
        var comparison = await CompareKeywordsAsync(left, right, years);
        if (!comparison.Success)
        {
            return ServiceResult<ResearcherReportResponse>.Fail(comparison.Error!);
        }

        return BuildReport("KeywordComparison", "Keyword comparison report", comparison.Data!);
    }

    public async Task<ServiceResult<ResearcherReportResponse>> GetJournalSummaryReportAsync(
        int? journalId,
        string? journalName,
        int years)
    {
        try
        {
            if (!journalId.HasValue && string.IsNullOrWhiteSpace(journalName))
            {
                return ServiceResult<ResearcherReportResponse>.Fail("Either journalId or journalName is required.");
            }

            if (years < 1 || years > 50)
            {
                return ServiceResult<ResearcherReportResponse>.Fail("years must be between 1 and 50.");
            }

            var data = await _researcherRepository.GetJournalSummaryAsync(journalId, journalName, years);
            return BuildReport("JournalPublicationSummary", "Journal publication summary", data);
        }
        catch (Exception ex)
        {
            return ServiceResult<ResearcherReportResponse>.Fail(
                "An error occurred while generating journal summary report: " + ex.Message);
        }
    }

    private static ServiceResult<ResearcherReportResponse> BuildReport(string reportType, string title, object data)
    {
        return ServiceResult<ResearcherReportResponse>.Ok(new ResearcherReportResponse
        {
            ReportType = reportType,
            Title = title,
            GeneratedAt = DateTime.UtcNow,
            Data = data
        });
    }

    private static string? ValidateWindow(int years, int topN)
    {
        if (years < 1 || years > 50)
        {
            return "years must be between 1 and 50.";
        }

        if (topN < 1 || topN > 100)
        {
            return "topN must be between 1 and 100.";
        }

        return null;
    }

    private static string? ValidateComparisonInput(string left, string right, int years)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return "Both left and right targets are required.";
        }

        if (left.Trim().Equals(right.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return "left and right must be different targets.";
        }

        if (years < 1 || years > 50)
        {
            return "years must be between 1 and 50.";
        }

        return null;
    }

    private static string NormalizeTargetType(string targetType)
    {
        var normalized = targetType.Trim();
        if (normalized.Equals("Topic", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("ResearchTopic", StringComparison.OrdinalIgnoreCase))
        {
            return "ResearchTopic";
        }

        if (normalized.Equals("Keyword", StringComparison.OrdinalIgnoreCase))
        {
            return "Keyword";
        }

        if (normalized.Equals("Journal", StringComparison.OrdinalIgnoreCase))
        {
            return "Journal";
        }

        return normalized;
    }
}
