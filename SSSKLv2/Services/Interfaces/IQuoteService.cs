using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Services.Interfaces
{
    public interface IQuoteService
    {
        Task<IEnumerable<QuoteDto>> GetQuotesAsync(int skip = 0, int take = 15, string? userId = null, string? targetUserId = null);
        Task<QuoteDto?> GetQuoteByIdAsync(Guid id, string? userId = null);
        Task<QuoteDto> CreateQuoteAsync(QuoteCreateDto dto, string creatorId);
        Task<QuoteDto?> UpdateQuoteAsync(Guid id, QuoteUpdateDto dto, string userId);
        Task<bool> DeleteQuoteAsync(Guid id, string userId);
        Task<bool> ToggleVoteAsync(Guid quoteId, string userId);
    }
}
