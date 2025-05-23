using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RazorDirectiveAttribute : Attribute
{
  public RazorDirectiveAttribute([NotNull] string directive) => this.Directive = directive;

  [NotNull]
  public string Directive { get; }
}

