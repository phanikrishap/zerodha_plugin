using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AspMvcSuppressViewErrorAttribute : Attribute
{
}

