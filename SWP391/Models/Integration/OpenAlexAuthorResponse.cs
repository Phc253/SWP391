using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SWP391.Models.Integration
{
    public class OpenAlexAuthorSearchResponse
    {
        [JsonPropertyName("results")]
        public List<OpenAlexAuthorData>? Results { get; set; }
    }

    public class OpenAlexAuthorData
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("works_count")]
        public int? WorksCount { get; set; }

        [JsonPropertyName("cited_by_count")]
        public int? CitedByCount { get; set; }

        [JsonPropertyName("last_known_institutions")]
        public List<InstitutionData>? LastKnownInstitutions { get; set; }
    }

    public class InstitutionData
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
        
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
