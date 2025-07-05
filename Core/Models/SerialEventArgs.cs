using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace CheapSerial.Core.Models
{
    #region Event Delegates

    /// <summary>
    /// Async event handler for connection status changes
    /// </summary>
    public delegate Task ConnectionStatusChangedHandler(object sender, ConnectionStatusChangedEventArgs e);

    /// <summary>
    /// Async event handler for data received events
    /// </summary>
    public delegate Task SerialDataReceivedHandler(object sender, SerialDataReceivedEventArgs e);

    /// <summary>
    /// Async event handler for pin change events
    /// </summary>
    public delegate Task SerialPinChangedHandler(object sender, SerialPinChangedEventArgs e);

    #endregion

    #region Event Arguments

    /// <summary>
    /// Event arguments for connection status changes
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public string PortName { get; }
        public bool IsConnected { get; }

        public ConnectionStatusChangedEventArgs(string portName, bool isConnected)
        {
            PortName = portName;
            IsConnected = isConnected;
        }
    }

    /// <summary>
    /// Event arguments for received data
    /// </summary>
    public class SerialDataReceivedEventArgs : EventArgs
    {
        public string PortName { get; }
        public byte[] Data { get; }

        public SerialDataReceivedEventArgs(string portName, byte[] data)
        {
            PortName = portName;
            Data = data;
        }
    }

    /// <summary>
    /// Event arguments for pin state changes (voltage monitoring)
    /// </summary>
    public class SerialPinChangedEventArgs : EventArgs
    {
        public string PortName { get; }
        public SerialPinChange EventType { get; }
        public SerialPinStates CurrentPinStates { get; }

        public SerialPinChangedEventArgs(string portName, SerialPinChange eventType, SerialPinStates pinStates)
        {
            PortName = portName;
            EventType = eventType;
            CurrentPinStates = pinStates;
        }
    }

    #endregion
}