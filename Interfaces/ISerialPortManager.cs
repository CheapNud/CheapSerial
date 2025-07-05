using CheapSerial.Core.Enums;
using CheapSerial.Core.Models;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace CheapSerial.Interfaces
{
    /// <summary>
    /// Interface for serial port management with async events
    /// </summary>
    public interface ISerialPortManager : IDisposable
    {
        string PortName { get; }
        bool IsConnected { get; }
        int TimeoutMs { get; set; }
        int ReconnectDelayMs { get; set; }
        SerialPort SerialPort { get; }

        event ConnectionStatusChangedHandler ConnectionStatusChanged;
        event SerialDataReceivedHandler DataReceived;
        event SerialPinChangedHandler PinChanged;

        void SetPort(string portName, int baudRate = (int)BaudRate.Baud115200, StopBits stopBits = StopBits.One,
            Parity parity = Parity.None, DataBits dataBits = DataBits.Eight);
        void SetPort(string portName, BaudRate baudRate, StopBits stopBits = StopBits.One,
            Parity parity = Parity.None, DataBits dataBits = DataBits.Eight);
        void SetReadStrategy(ReadStrategy strategy, bool enableSyncFallback = true, int syncFallbackTimeoutMs = 100);
        Task<bool> ConnectAsync();
        void Disconnect();
        Task<bool> SendAsync(byte[] data);
        Task<bool> SendAsync(string text);

        // Voltage/Signal control methods
        void SetDtr(bool enable);                           // Control DTR voltage line (~12V when true)
        void SetRts(bool enable);                           // Control RTS voltage line (~12V when true)
        void SetBreakState(bool enable);                    // Control break signal
        bool GetDtr();                                      // Get current DTR state
        bool GetRts();                                      // Get current RTS state  
        bool GetBreakState();                               // Get current break state
        SerialPinStates GetPinStates();                     // Get all pin states (CTS, DSR, CD, etc.)
        void SetVoltageConfiguration(bool dtrEnable, bool rtsEnable, bool breakState = false);
        int[] GetSupportedBaudRates();                      // Get supported baud rates for this port
    }
}