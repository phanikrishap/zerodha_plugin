using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AspMvcPartialViewAttribute : Attribute
{
}

