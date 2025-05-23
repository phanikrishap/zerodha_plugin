using System;

#nullable disable
namespace QANinjaAdapter.Annotations;

[Obsolete("Use [ContractAnnotation('=> halt')] instead")]
[AttributeUsage(AttributeTargets.Method)]
public sealed class TerminatesProgramAttribute : Attribute
{
}

