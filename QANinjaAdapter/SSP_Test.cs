using System;
using System.IO;
using System.Threading;
using QANinjaAdapter;
using QANinjaAdapter.SyntheticInstruments;
using QANinjaAdapter.Models.MarketData;

namespace QANinjaAdapter.Test
{
    /// <summary>
    /// A simple test class to demonstrate the Synthetic Straddle Processor functionality.
    /// </summary>
    public class SSP_Test
    {
        private QAAdapter _qaAdapter;
        private SyntheticStraddleService _syntheticStraddleService;

        /// <summary>
        /// Initializes the test environment.
        /// </summary>
        public void Initialize()
        {
            Console.WriteLine("Initializing SSP Test...");
            
            // Create QAAdapter instance (normally this would be created by NinjaTrader)
            _qaAdapter = new QAAdapter();
            
            // Create SyntheticStraddleService instance
            _syntheticStraddleService = new SyntheticStraddleService(_qaAdapter);
            
            // Load straddle configurations from Documents\NinjaTrader 8\QAAdapter directory
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string configFilePath = Path.Combine(documentsPath, "NinjaTrader 8", "QAAdapter", "straddles_config.json");
            _syntheticStraddleService.LoadStraddleConfigs(configFilePath);
            
            Console.WriteLine("SSP Test initialized successfully.");
        }

        /// <summary>
        /// Simulates receiving ticks for the straddle legs.
        /// </summary>
        public void SimulateTicks()
        {
            Console.WriteLine("Simulating ticks for straddle legs...");
            
            // Define the straddle legs
            string ceSymbol = "NIFTY2460725000CE";
            string peSymbol = "NIFTY2460725000PE";
            
            // Check if these are valid leg instruments
            if (!_syntheticStraddleService.IsLegInstrument(ceSymbol) || 
                !_syntheticStraddleService.IsLegInstrument(peSymbol))
            {
                Console.WriteLine("Error: One or both of the leg symbols are not defined in the straddle configuration.");
                return;
            }
            
            // Simulate a tick for the CE leg
            Console.WriteLine($"Simulating tick for CE leg: {ceSymbol}");
            SimulateTick(ceSymbol, 150.25, 10);
            
            // Wait a moment
            Thread.Sleep(1000);
            
            // Simulate a tick for the PE leg
            Console.WriteLine($"Simulating tick for PE leg: {peSymbol}");
            SimulateTick(peSymbol, 120.75, 5);
            
            // Wait a moment
            Thread.Sleep(1000);
            
            // Simulate another tick for the CE leg with a price change
            Console.WriteLine($"Simulating another tick for CE leg: {ceSymbol} with price change");
            SimulateTick(ceSymbol, 152.50, 15);
            
            Console.WriteLine("Tick simulation completed.");
        }

        /// <summary>
        /// Simulates a tick for a specific instrument.
        /// </summary>
        /// <param name="symbol">The instrument symbol</param>
        /// <param name="price">The tick price</param>
        /// <param name="quantity">The tick quantity</param>
        private void SimulateTick(string symbol, double price, int quantity)
        {
            // Create a Tick object
            var tick = new Tick
            {
                InstrumentSymbol = symbol,
                Price = price,
                Volume = quantity,
                Timestamp = DateTime.Now,
                Type = TickType.Last
            };
            
            // Process the tick
            _syntheticStraddleService.ProcessLegTick(tick);
        }

        /// <summary>
        /// Simulates receiving ticks directly from Zerodha.
        /// </summary>
        public void SimulateZerodhaTicks()
        {
            Console.WriteLine("Simulating Zerodha ticks for straddle legs...");
            
            // Define the straddle legs
            string ceSymbol = "NIFTY2460725000CE";
            string peSymbol = "NIFTY2460725000PE";
            
            // Simulate a Zerodha tick for the CE leg
            Console.WriteLine($"Simulating Zerodha tick for CE leg: {ceSymbol}");
            SimulateZerodhaTick(ceSymbol, 150.25, 10);
            
            // Wait a moment
            Thread.Sleep(1000);
            
            // Simulate a Zerodha tick for the PE leg
            Console.WriteLine($"Simulating Zerodha tick for PE leg: {peSymbol}");
            SimulateZerodhaTick(peSymbol, 120.75, 5);
            
            Console.WriteLine("Zerodha tick simulation completed.");
        }

        /// <summary>
        /// Simulates a Zerodha tick for a specific instrument.
        /// </summary>
        /// <param name="symbol">The instrument symbol</param>
        /// <param name="price">The tick price</param>
        /// <param name="quantity">The tick quantity</param>
        private void SimulateZerodhaTick(string symbol, double price, int quantity)
        {
            // Create a ZerodhaTickData object
            var tickData = new ZerodhaTickData
            {
                InstrumentIdentifier = symbol,
                LastTradePrice = price,
                LastTradeQty = quantity,
                TotalQtyTraded = quantity,
                LastTradeTime = DateTime.Now
            };
            
            // Process the tick through QAAdapter
            _qaAdapter.ProcessParsedTick(symbol, tickData);
        }

        /// <summary>
        /// Main entry point for the test.
        /// </summary>
        public static void Main(string[] args)
        {
            var test = new SSP_Test();
            
            // Initialize the test environment
            test.Initialize();
            
            // Simulate ticks directly to the SyntheticStraddleService
            test.SimulateTicks();
            
            // Simulate ticks through the QAAdapter (as they would come from Zerodha)
            test.SimulateZerodhaTicks();
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
