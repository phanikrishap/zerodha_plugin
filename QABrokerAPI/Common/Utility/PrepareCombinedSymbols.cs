#nullable disable
namespace QABrokerAPI.Common.Utility;

public class PrepareCombinedSymbols
{
  public static string CombinedPartialDepth(string allPairs, string depth)
  {
    string[] strArray = allPairs.Split(',');
    for (int index = 0; index < strArray.Length; ++index)
      strArray[index] = $"{strArray[index].ToLower()}@depth{depth}/";
    return allPairs = string.Join("", strArray);
  }

  public static string CombinedDepth(string allPairs)
  {
    string[] strArray = allPairs.Split(',');
    for (int index = 0; index < strArray.Length; ++index)
      strArray[index] = strArray[index].ToLower() + "@depth/";
    return allPairs = string.Join("", strArray);
  }
}

