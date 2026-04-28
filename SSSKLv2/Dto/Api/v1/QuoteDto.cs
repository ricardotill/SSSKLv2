using System;
using System.Collections.Generic;

namespace SSSKLv2.Dto.Api.v1
{
    public class QuoteDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime DateSaid { get; set; }
        public DateTime CreatedOn { get; set; }
        public ApplicationUserDto? CreatedBy { get; set; }
        
        public IEnumerable<QuoteAuthorDto> Authors { get; set; } = new List<QuoteAuthorDto>();
        public IEnumerable<string> VisibleToRoles { get; set; } = new List<string>();
        
        public int VoteCount { get; set; }
        public int CommentsCount { get; set; }
        public bool HasVoted { get; set; }
    }
}
