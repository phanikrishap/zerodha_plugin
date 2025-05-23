using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class AspMvcMasterLocationFormatAttribute : Attribute
{
  public AspMvcMasterLocationFormatAttribute([NotNull] string format) => this.Format = format;

  [NotNull]
  public string Format { get; }
}

