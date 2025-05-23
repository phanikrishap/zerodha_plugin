using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AspMvcControllerAttribute : Attribute
{
  public AspMvcControllerAttribute()
  {
  }

  public AspMvcControllerAttribute([NotNull] string anonymousProperty)
  {
    this.AnonymousProperty = anonymousProperty;
  }

  [CanBeNull]
  public string AnonymousProperty { get; }
}

