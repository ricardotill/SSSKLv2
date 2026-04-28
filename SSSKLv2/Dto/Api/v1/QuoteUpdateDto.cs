using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SSSKLv2.Dto.Api.v1
{
    public class QuoteUpdateDto
    {
        [Required]
        public string Text { get; set; } = string.Empty;
        
        [Required]
        public DateTime DateSaid { get; set; }
        
        public IEnumerable<QuoteAuthorCreateDto> Authors { get; set; } = new List<QuoteAuthorCreateDto>();
        public IEnumerable<string> VisibleToRoles { get; set; } = new List<string>();
    }
}
