using System.Runtime.Serialization;

namespace DigiTrade.SharedKernel.Models.Response;

/// <summary>
/// Model for list result. 
/// </summary>
[DataContract]
public class ListResult<T>
{
    public ListResult() { }

    public ListResult(IEnumerable<T> items)
    {
        Items.AddRange(items);
    }
    public ListResult(long total, IEnumerable<T> items)
    {
        Total = total;
        Items.AddRange(items);
    }

    [DataMember]
    public List<T> Items { get; set; } = new();

    [DataMember]
    public long? Total { get; set; }
    [DataMember]
    public bool? HasNext { get; set; }
}