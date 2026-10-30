using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SSSKL.Contracts.Dto.Api.v1
{
    public class QuoteCreateDto
    {
        [Required]
        public string Text { get; set; } = string.Empty;
        
        [Required, JsonRequired]
        public DateTime DateSaid { get; set; }
        
        public IEnumerable<QuoteAuthorCreateDto> Authors { get; set; } = new List<QuoteAuthorCreateDto>();
        public IEnumerable<string> VisibleToRoles { get; set; } = new List<string>();
        [JsonRequired]
        public bool SendNotification { get; set; }
    }

    public class QuoteAuthorCreateDto
    {
        public string? ApplicationUserId { get; set; }
        public string? CustomName { get; set; }
    }
}
