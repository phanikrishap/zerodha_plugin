// Decompiled with JetBrains decompiler
// Type: QABrokerAPI.Common.Extensions.EnumExtensions
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Extensions;

public static class EnumExtensions
{
  public static string GetEnumMemberValue<T>(T value) where T : struct, IConvertible
  {
    MemberInfo element = typeof (T).GetTypeInfo().DeclaredMembers.SingleOrDefault<MemberInfo>((Func<MemberInfo, bool>) (x => x.Name == value.ToString()));
    if ((object) element == null)
      return (string) null;
    return element.GetCustomAttribute<EnumMemberAttribute>(false)?.Value;
  }
}
