using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using System;
using System.ComponentModel;

#nullable disable
namespace QANinjaAdapter
{
    [BrowsableProperty("User", false)]
    [BrowsableProperty("Password", false)]
    [EditorBrowsable(EditorBrowsableState.Always)]
    public class QAConnectorOptions : CustomConnectOptions
    {
        private string _connectionTimeOut = "800";
        private string _serverAddress;
        private string _port = "3900";
        private string _version = "3";
        private LogLevel _logLevel = LogLevel.Large;

        [DisplayName("Version")]
        [ReadOnly(true)]
        public string Version
        {
            get => this._version;
            set => this._version = value;
        }

        // Comment out the BrandName override to use the default naming from base class
        //[Browsable(false)]
        //public override string BrandName => "QA Adapter";

        [Browsable(false)]
        public override Type AdapterClassType => typeof(QAAdapter);

        [DisplayName("Connection Time Out (sec)")]
        public string ConnectionTimeOutSeconds
        {
            get => this._connectionTimeOut;
            set => this._connectionTimeOut = value;
        }

        [DisplayName("Server Address")]
        [TypeConverter(typeof(FormatStringConverter))]
        public string ServerAddress
        {
            get => this._serverAddress;
            set => this._serverAddress = value;
        }

        [DisplayName("Port")]
        public string Port
        {
            get => this._port;
            set => this._port = value;
        }

        [DisplayName("Log Level")]
        public LogLevel LogLevel
        {
            get => this._logLevel;
            set => this._logLevel = value;
        }

        public QAConnectorOptions()
        {
            try{

            this.Provider = Provider.Custom19;
            int num = this.IsDataProviderOnly ? 1 : 0;
            // Set the name consistently with what NinjaTrader expects
            this.Name = "QA Adapter";
            this.ServerAddress = "kite.zerodha.com";
            }
            catch (Exception ex)
            {
                // Handle exception if needed
                Console.WriteLine($"Error initializing QAConnectorOptions: {ex.Message}");
            }

        }
    }
}