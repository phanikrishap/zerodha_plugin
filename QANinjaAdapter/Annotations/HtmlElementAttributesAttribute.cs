using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class HtmlElementAttributesAttribute : Attribute
{
  public HtmlElementAttributesAttribute()
  {
  }

  public HtmlElementAttributesAttribute([NotNull] string name) => this.Name = name;

  [CanBeNull]
  public string Name { get; }
}

