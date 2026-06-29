namespace DigiTrade.Common.Models;

/// <summary>
/// Model for paging.
/// </summary>
public class PageHeaderModel
{
    public PageHeaderModel() { }
    public PageHeaderModel(int currentPage, int itemsPerPage, int totalItems)
    {
        Page = currentPage;
        PageSize = itemsPerPage;
        ItemsCount = totalItems;
    }

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int ItemsCount { get; set; }
}