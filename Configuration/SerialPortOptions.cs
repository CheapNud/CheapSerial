using CheapSerial.Core.Enums;
using System.IO.Ports;

namespace CheapSerial.Configuration
{
    /// <summary>
    /// Configuration options for serial port connections
    /// </summary>
    public class SerialPortOptions
    {
        public const string SectionName = "SerialPort";

        public string PortName { get; set; } = "";
        public int BaudRate { get; set; } = (int)Core.Enums.BaudRate.Baud115200;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Parity Parity { get; set; } = Parity.None;
        public DataBits DataBits { get; set; } = DataBits.Eight;
        public int TimeoutMs { get; set; } = 5000;
        public int ReconnectDelayMs { get; set; } = 1000;
        public bool AutoConnect { get; set; } = true;

        // Fallback strategy configuration
        public ReadStrategy ReadStrategy { get; set; } = ReadStrategy.AsyncWithSyncFallback;
        public bool EnableSyncFallback { get; set; } = true;
        public int SyncFallbackTimeoutMs { get; set; } = 100;
        public int AsyncRetryCount { get; set; } = 1;
        public bool LogFallbackEvents { get; set; } = true;
        public int SuccessfulReadsBeforeAsyncPromotion { get; set; } = 10;

        // Voltage/Signal control configuration
        public bool DtrEnable { get; set; } = false;           // Data Terminal Ready - can provide ~12V
        public bool RtsEnable { get; set; } = false;           // Request To Send - can provide ~12V  
        public bool InitialBreakState { get; set; } = false;   // Break signal state on connection
        public bool AutoSetDtrOnConnect { get; set; } = false; // Automatically set DTR high on connect
        public bool AutoSetRtsOnConnect { get; set; } = false; // Automatically set RTS high on connect
        public bool MonitorPinChanges { get; set; } = false;   // Monitor CTS, DSR, CD pin changes
        public int PinChangeDebounceMs { get; set; } = 50;     // Debounce pin change events
    }
}