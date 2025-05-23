using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ContractAnnotationAttribute : Attribute
{
  public ContractAnnotationAttribute([NotNull] string contract)
    : this(contract, false)
  {
  }

  public ContractAnnotationAttribute([NotNull] string contract, bool forceFullStates)
  {
    this.Contract = contract;
    this.ForceFullStates = forceFullStates;
  }

  [NotNull]
  public string Contract { get; }

  public bool ForceFullStates { get; }
}

