using System.Runtime.Serialization;

namespace DigiTrade.SharedKernel.Models.Response;

/// <summary>
/// Model for list result. 
/// </summary>
[DataContract]
public class FilterListResult<TF, T> : ListResult<T>
{
    public FilterListResult()
    { }

    public FilterListResult(IEnumerable<T> items) : base(items) { }

    [DataMember]
    public TF? Filter { get; set; }
}