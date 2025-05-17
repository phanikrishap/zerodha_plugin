using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable disable
namespace QANinjaAdapter.ViewModels;

public class ViewModelBase : INotifyPropertyChanged
{
  private bool _isBusy;
  private string _message;
  private string _subMessage;

  public bool IsBusy
  {
    get => this._isBusy;
    set
    {
      if (this._isBusy == value)
        return;
      this._isBusy = value;
      this.OnPropertyChanged(nameof (IsBusy));
    }
  }

  public string Message
  {
    get => this._message;
    set
    {
      if (!(this._message != value))
        return;
      this._message = value;
      this.OnPropertyChanged(nameof (Message));
    }
  }

  public string SubMessage
  {
    get => this._subMessage;
    set
    {
      if (!(this._subMessage != value))
        return;
      this._subMessage = value;
      this.OnPropertyChanged(nameof (SubMessage));
    }
  }

  public bool IsDeleted { get; set; }

  public bool IsModified { get; set; }

  public bool IsLoaded { get; set; }

  protected ViewModelBase()
  {
    this.IsModified = false;
    this.IsLoaded = false;
    this.IsDeleted = false;
  }

  public event PropertyChangedEventHandler PropertyChanged;

  protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
  {
    PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
    if (propertyChanged == null)
      return;
    propertyChanged((object) this, new PropertyChangedEventArgs(propertyName));
  }
}
