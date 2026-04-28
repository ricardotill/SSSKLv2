using System;

namespace SSSKLv2.Dto.Api.v1
{
    public class QuoteAuthorDto
    {
        public Guid Id { get; set; }
        public string? ApplicationUserId { get; set; }
        public ApplicationUserDto? ApplicationUser { get; set; }
        public string? CustomName { get; set; }
    }
}
