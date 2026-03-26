namespace SSSKLv2.Dto.Api.v1;

public class PaginationObject<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
}