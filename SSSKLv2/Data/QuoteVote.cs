using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKLv2.Data;

public class QuoteVote : BaseModel
{
    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
