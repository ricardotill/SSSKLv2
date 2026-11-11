using System.ComponentModel;

namespace SSSKL.Domain.Entities;

public class Announcement : BaseModel
{
    [DisplayName("Bericht")]
    public string Message { get; set; } = string.Empty;
    
    [DisplayName("Beschrijving")]
    public string? Description { get; set; }
    
    [DisplayName("Foto URL")]
    public string? FotoUrl { get; set; }
    
    [DisplayName("URL")]
    public string? Url { get; set; }
    
    [DisplayName("Volgorde")]
    public int Order { get; set; }
    
    [DisplayName("Ingeroosterd")]
    public bool IsScheduled { get; set; } = false;
    
    [DisplayName("Vanaf")]
    public DateTime? PlannedFrom { get; set; }
    
    [DisplayName("Tot")]
    public DateTime? PlannedTill { get; set; }
}