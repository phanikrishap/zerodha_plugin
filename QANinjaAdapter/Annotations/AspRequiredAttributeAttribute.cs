using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AspRequiredAttributeAttribute : System.Attribute
{
  public AspRequiredAttributeAttribute([NotNull] string attribute) => this.Attribute = attribute;

  [NotNull]
  public string Attribute { get; }
}

