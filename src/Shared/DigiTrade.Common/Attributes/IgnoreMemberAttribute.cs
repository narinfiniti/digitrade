namespace DigiTrade.Common.Attributes;

/// <summary>
/// Marker attribute to ignore class member.
/// </summary>
/// <see>
/// https://github.com/jhewlett/ValueObject
/// </see>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class IgnoreMemberAttribute : Attribute { }