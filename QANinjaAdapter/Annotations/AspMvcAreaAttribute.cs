using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AspMvcAreaAttribute : Attribute
{
  public AspMvcAreaAttribute()
  {
  }

  public AspMvcAreaAttribute([NotNull] string anonymousProperty)
  {
    this.AnonymousProperty = anonymousProperty;
  }

  [CanBeNull]
  public string AnonymousProperty { get; }
}

