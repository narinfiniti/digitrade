namespace DigiTrade.Common.Models;

public class TupleResult(bool isOk, string[] errors)
{
    public bool Succeeded { get; set; } = isOk;
    public string[] Errors { get; set; } = errors;
}