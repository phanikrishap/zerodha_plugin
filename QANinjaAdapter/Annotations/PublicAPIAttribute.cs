using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
public sealed class PublicAPIAttribute : Attribute
{
  public PublicAPIAttribute()
  {
  }

  public PublicAPIAttribute([NotNull] string comment) => this.Comment = comment;

  [CanBeNull]
  public string Comment { get; }
}

