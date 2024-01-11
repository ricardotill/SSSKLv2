using System.ComponentModel;

namespace SSSKLv2.Data;

public class Announcement : BaseModel
{
    [Microsoft.Build.Framework.Required]
    [DisplayName("Bericht")]
    public string Message { get; set; }
    
    [DisplayName("Beschrijving")]
    public string? Description { get; set; }
    
    [DisplayName("Foto URL")]
    public string? FotoUrl { get; set; }
    
    [DisplayName("URL")]
    public string? Url { get; set; }
    
    [DisplayName("Volgorde")]
    public int Order { get; set; }
    
    [Microsoft.Build.Framework.Required]
    [DisplayName("Ingeroosterd")]
    public bool IsScheduled { get; set; } = false;
    
    [DisplayName("Vanaf")]
    public DateTime? PlannedFrom { get; set; }
    
    [DisplayName("Tot")]
    public DateTime? PlannedTill { get; set; }
}