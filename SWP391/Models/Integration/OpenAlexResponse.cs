using System.Text.Json.Serialization;

namespace SWP391.Models.Integration
{
    public class OpenAlexResponse
    {
        [JsonPropertyName("meta")]
        public MetaData? Meta { get; set; }

        [JsonPropertyName("results")]
        public List<WorkData>? Results { get; set; }
    }

    public class MetaData
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        
        [JsonPropertyName("db_response_time_ms")]
        public int DbResponseTimeMs { get; set; }
        
        [JsonPropertyName("page")]
        public int Page { get; set; }
        
        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }
    }

    public class WorkData
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("publication_year")]
        public int? PublicationYear { get; set; }

        [JsonPropertyName("abstract_inverted_index")]
        public Dictionary<string, List<int>>? AbstractInvertedIndex { get; set; }
        
        [JsonPropertyName("authorships")]
        public List<Authorship>? Authorships { get; set; }

        [JsonPropertyName("primary_location")]
        public Location? PrimaryLocation { get; set; }

        [JsonPropertyName("concepts")]
        public List<Concept>? Concepts { get; set; }
    }

    public class Authorship
    {
        [JsonPropertyName("author")]
        public AuthorData? Author { get; set; }
    }

    public class AuthorData
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }

    public class Location
    {
        [JsonPropertyName("source")]
        public SourceData? Source { get; set; }
    }

    public class SourceData
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("issn_l")]
        public string? Issn { get; set; }

        [JsonPropertyName("host_organization_name")]
        public string? Publisher { get; set; }
    }

    public class Concept
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
        
        [JsonPropertyName("score")]
        public float Score { get; set; }
    }
}