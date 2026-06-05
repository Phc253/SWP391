namespace SWP391.Models.Account
{
    public static class UserActorTypes
    {
        public const string Researcher = "Researcher";
        public const string Lecturer = "Lecturer";
        public const string Student = "Student";

        public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(
            new[] { Researcher, Lecturer, Student },
            StringComparer.OrdinalIgnoreCase);

        public static string Normalize(string? actorType)
        {
            if (string.IsNullOrWhiteSpace(actorType))
            {
                return Student;
            }

            var trimmed = actorType.Trim();
            return Allowed.FirstOrDefault(a => string.Equals(a, trimmed, StringComparison.OrdinalIgnoreCase))
                ?? string.Empty;
        }
    }
}
