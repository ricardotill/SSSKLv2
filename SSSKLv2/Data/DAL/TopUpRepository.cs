using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class TopUpRepository(IDbContextFactory<ApplicationDbContext> _dbContextFactory) : ITopUpRepository
{
    public async Task<PaginationObject<TopUp>> GetAllPagination(int page)
    {
        var pagination = new PaginationObject<TopUp>();
        using (var context = _dbContextFactory.CreateDbContext())
        {
            pagination.TotalObjects = await context.TopUp.CountAsync();
            pagination.Value = await context.TopUp
                // Use AsNoTracking to disable EF change tracking
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedOn)
                .Skip(page * 5)
                .Take(5).ToListAsync();
        }

        return pagination;
    }
    
    public async Task<PaginationObject<TopUp>> GetPersonalPagination(int page, string id)
    {
        var pagination = new PaginationObject<TopUp>();
        using (var context = _dbContextFactory.CreateDbContext())
        {
            pagination.TotalObjects = await context.TopUp.CountAsync();
            pagination.Value = await context.TopUp
                // Use AsNoTracking to disable EF change tracking
                .AsNoTracking()
                .Where(x => x.User.Id == id)
                .OrderByDescending(x => x.CreatedOn)
                .Skip(page * 5)
                .Take(5).ToListAsync();
        }

        return pagination;
    }
    
    public async Task<TopUp> GetById(Guid id)
    {
        using (var context = await _dbContextFactory.CreateDbContextAsync())
        {
            var topup = await context.TopUp.FindAsync(id);
            if (topup != null)
            {
                return topup;
            }

            throw new NotFoundException("TopUp not found");
        }
    }

    public Task<IEnumerable<TopUp>> GetAll()
    {
        throw new NotImplementedException();
    }

    public async Task Create(TopUp obj)
    {
        using (var context = _dbContextFactory.CreateDbContext())
        {
            var entry = await context.TopUp.FindAsync(obj.Id);
            if (entry == null)
            {
                context.TopUp.Add(obj);
                await context.SaveChangesAsync();
            }
            else throw new NotFoundException("TopUp Not Found");
        }    
    }

    public Task Update(TopUp obj)
    {
        throw new NotImplementedException();
    }

    public async Task Delete(Guid id)
    {
        using (var context = _dbContextFactory.CreateDbContext())
        {
            var entry = await context.TopUp.FindAsync(id);
            if (entry != null)
            {
                context.TopUp.Remove(entry);
                await context.SaveChangesAsync();
            }
            else throw new NotFoundException("TopUp Not Found");
        }    
    }
}