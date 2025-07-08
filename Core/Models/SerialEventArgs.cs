using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace CheapSerial.Core.Models
{
    /// <summary>
    /// Event arguments for pin state changes (voltage monitoring)
    /// </summary>
    public class SerialPinChangedEventArgs(string portName, SerialPinChange eventType, SerialPinStates pinStates) : EventArgs
    {
        /// <summary>
        /// Async event handler for pin change events
        /// </summary>
        public delegate Task SerialPinChangedHandler(object sender, SerialPinChangedEventArgs e);
        public string PortName { get; } = portName;
        public SerialPinChange EventType { get; } = eventType;
        public SerialPinStates CurrentPinStates { get; } = pinStates;
    }
}