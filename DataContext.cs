using QANinjaAdapter.Classes.Binance.Symbols;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable disable
namespace QANinjaAdapter;

public class DataContext : INotifyPropertyChanged
{
  private List<string> _darkSkins = new List<string>()
  {
    "Dark",
    "Slate Gray",
    "Slate Dark"
  };
  private static DataContext _instance;

  public static DataContext Instance
  {
    get
    {
      if (DataContext._instance == null)
        DataContext._instance = new DataContext();
      return DataContext._instance;
    }
  }

  public Dictionary<string, string> SymbolNames { set; get; }

  public ObservableCollection<SymbolObject> Instruments { set; get; }

  public DataContext()
  {
    this.Instruments = new ObservableCollection<SymbolObject>();
    this.SymbolNames = new Dictionary<string, string>();
  }

  public string GetOriginalName(Instrument instrument)
  {
    if (instrument.MasterInstrument.InstrumentType != InstrumentType.Option)
    {
      string str;
      return this.SymbolNames.TryGetValue(instrument.MasterInstrument.Name, out str) ? str : string.Empty;
    }
    string str1 = "P";
    if (instrument.OptionRight == OptionRight.Call)
      str1 = "C";
    string str2 = instrument.StrikePrice.ToString().Replace(',', '.');
    return $".{instrument.MasterInstrument.Name}{instrument.Expiry:yyMMdd}{str1}{str2}";
  }

  public bool IsDarkSkin() => this._darkSkins.Contains(Globals.GeneralOptions.Skin);

  public event PropertyChangedEventHandler PropertyChanged;

  protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
  {
    PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
    if (propertyChanged == null)
      return;
    propertyChanged((object) this, new PropertyChangedEventArgs(propertyName));
  }
}
