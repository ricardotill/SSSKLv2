namespace SSSKLv2.Dto.Api.v1;

public class GlobalSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedOn { get; set; }
}

public class GlobalSettingUpdateDto
{
    public string Value { get; set; } = string.Empty;
}
