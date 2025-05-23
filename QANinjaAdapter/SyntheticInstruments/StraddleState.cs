using System;
using System.Collections.Generic;

namespace QANinjaAdapter.SyntheticInstruments
{
    /// <summary>
    /// Holds the dynamic, real-time data for each active synthetic straddle.
    /// It specifically tracks the last known price for each leg.
    /// </summary>
    public class StraddleState
    {
        /// <summary>
        /// The static definition of this straddle.
        /// </summary>
        public StraddleDefinition Definition { get; }

        /// <summary>
        /// The last known price of the Call option leg.
        /// </summary>
        public double LastCEPrice { get; set; }

        /// <summary>
        /// The last known price of the Put option leg.
        /// </summary>
        public double LastPEPrice { get; set; }

        /// <summary>
        /// Timestamp of the last received tick for the Call leg.
        /// </summary>
        public DateTime LastCETimestamp { get; set; }

        /// <summary>
        /// Timestamp of the last received tick for the Put leg.
        /// </summary>
        public DateTime LastPETimestamp { get; set; }
        
        /// <summary>
        /// The last known volume of the Call option leg.
        /// </summary>
        public long LastCEVolume { get; set; }
        
        /// <summary>
        /// The last known volume of the Put option leg.
        /// </summary>
        public long LastPEVolume { get; set; }
        
        /// <summary>
        /// Flag indicating whether the CE leg's volume has been incorporated into a synthetic tick.
        /// </summary>
        public bool CEVolumeIncorporated { get; set; }
        
        /// <summary>
        /// Flag indicating whether the PE leg's volume has been incorporated into a synthetic tick.
        /// </summary>
        public bool PEVolumeIncorporated { get; set; }
        
        /// <summary>
        /// List of recent ticks for the CE leg (up to 5).
        /// </summary>
        public List<Tick> RecentCETicks { get; private set; }
        
        /// <summary>
        /// List of recent ticks for the PE leg (up to 5).
        /// </summary>
        public List<Tick> RecentPETicks { get; private set; }

        /// <summary>
        /// Indicates if at least one tick has been received for the Call leg.
        /// </summary>
        public bool HasCEData { get; set; }

        /// <summary>
        /// Indicates if at least one tick has been received for the Put leg.
        /// </summary>
        public bool HasPEData { get; set; }
        
        /// <summary>
        /// The last volume we injected for the synthetic instrument.
        /// </summary>
        public long LastSyntheticVolume { get; set; }
        
        /// <summary>
        /// The cumulative volume for the synthetic instrument for the current session.
        /// </summary>
        public long CumulativeSyntheticVolume { get; set; }

        /// <summary>
        /// Creates a new instance of the StraddleState class.
        /// </summary>
        /// <param name="definition">The straddle definition.</param>
        public StraddleState(StraddleDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            // Initialize prices to 0.0 or a safe 'NaN' equivalent if preferred,
            // but the HasCEData/HasPEData flags will prevent calculation until both are true.
            LastCEPrice = 0.0;
            LastPEPrice = 0.0;
            LastCETimestamp = DateTime.MinValue;
            LastPETimestamp = DateTime.MinValue;
            LastCEVolume = 0;
            LastPEVolume = 0;
            CEVolumeIncorporated = true; // Initially true since there's no volume to incorporate
            PEVolumeIncorporated = true; // Initially true since there's no volume to incorporate
            HasCEData = false;
            HasPEData = false;
            LastSyntheticVolume = 0;
            CumulativeSyntheticVolume = 0;
            RecentCETicks = new List<Tick>(5); // Store up to 5 recent ticks
            RecentPETicks = new List<Tick>(5); // Store up to 5 recent ticks
        }

        /// <summary>
        /// Gets the current synthetic straddle price (sum of last known leg prices).
        /// Returns 0.0 if both legs haven't reported data yet.
        /// </summary>
        public double GetSyntheticPrice()
        {
            if (HasCEData && HasPEData)
            {
                return LastCEPrice + LastPEPrice;
            }
            return 0.0; // Or throw an exception, or return NaN if preferred
        }
    }
}
