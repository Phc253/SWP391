namespace SWP391.Models.Admin
{
    public class SystemSettingResponse
    {
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
    }

    public class UpdateSettingRequest
    {
        public string? Value { get; set; }
    }
}
