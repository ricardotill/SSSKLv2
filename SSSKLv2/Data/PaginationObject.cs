namespace SSSKLv2.Data;

public class PaginationObject<T>
{
    public IList<T> Value { get; set; } = new List<T>();
    public int TotalObjects { get; set; } = 0;
}