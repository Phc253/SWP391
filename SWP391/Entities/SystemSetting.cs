using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class SystemSetting
{
    public string SettingKey { get; set; } = null!;

    public string? SettingValue { get; set; }
}
