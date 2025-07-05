using System;

namespace CheapSerial.Core.Models
{
    /// <summary>
    /// Pin states for voltage monitoring
    /// </summary>
    public class SerialPinStates
    {
        public bool CtsHolding { get; set; }    // Clear To Send
        public bool DsrHolding { get; set; }    // Data Set Ready  
        public bool CDHolding { get; set; }     // Carrier Detect
        public bool RingIndicator { get; set; } // Ring Indicator
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}