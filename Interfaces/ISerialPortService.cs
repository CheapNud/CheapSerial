using CheapSerial.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CheapSerial.Core.Models.ConnectionStatusChangedEventArgs;
using static CheapSerial.Core.Models.SerialDataReceivedEventArgs;
using static CheapSerial.Core.Models.SerialPinChangedEventArgs;

namespace CheapSerial.Interfaces
{
    /// <summary>
    /// Service interface for managing multiple serial ports with async events
    /// </summary>
    public interface ISerialPortService : IDisposable
    {
        event ConnectionStatusChangedHandler ConnectionStatusChanged;
        event SerialDataReceivedHandler DataReceived;
        event SerialPinChangedHandler PinChanged;

        IEnumerable<string> GetPortNames();
        Task<bool> ConnectAsync(string portName);
        void Disconnect(string portName);
        void DisconnectAll();
        bool IsConnected(string portName);
        Task<bool> SendAsync(string portName, byte[] data);
        Task<bool> SendAsync(string portName, string text);
        ISerialPortManager GetManager(string portName);

        // Voltage/Signal control for multiple ports
        void SetDtr(string portName, bool enable);
        void SetRts(string portName, bool enable);
        void SetBreakState(string portName, bool enable);
        bool GetDtr(string portName);
        bool GetRts(string portName);
        SerialPinStates GetPinStates(string portName);
        void SetVoltageConfiguration(string portName, bool dtrEnable, bool rtsEnable, bool breakState = false);
        string[] GetAvailableComPorts();                    // Get all available COM ports on system
    }
}