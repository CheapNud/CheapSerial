using CheapSerial.Core.Enums;
using System.IO.Ports;

namespace CheapSerial.Configuration
{
    /// <summary>
    /// Predefined serial port configurations for common devices
    /// </summary>
    public static class SerialConfigurations
    {
        /// <summary>
        /// Standard Arduino configuration (9600 baud, 8N1)
        /// </summary>
        public static SerialPortOptions Arduino(string portName) => new()
        {
            PortName = portName,
            BaudRate = (int)BaudRate.Baud9600,
            StopBits = StopBits.One,
            Parity = Parity.None,
            DataBits = DataBits.Eight,
            DtrEnable = true,               // Arduino resets on DTR
            AutoSetDtrOnConnect = true,
            MonitorPinChanges = true,
            ReadStrategy = ReadStrategy.AsyncWithSyncFallback
        };

        /// <summary>
        /// High-speed Arduino configuration (115200 baud)
        /// </summary>
        public static SerialPortOptions ArduinoHighSpeed(string portName) => new()
        {
            PortName = portName,
            BaudRate = (int)BaudRate.Baud115200,
            StopBits = StopBits.One,
            Parity = Parity.None,
            DataBits = DataBits.Eight,
            DtrEnable = true,
            AutoSetDtrOnConnect = true,
            MonitorPinChanges = true,
            ReadStrategy = ReadStrategy.AsyncWithSyncFallback
        };

        /// <summary>
        /// ESP32/ESP8266 configuration (115200 baud)
        /// </summary>
        public static SerialPortOptions ESP32(string portName) => new()
        {
            PortName = portName,
            BaudRate = (int)BaudRate.Baud115200,
            StopBits = StopBits.One,
            Parity = Parity.None,
            DataBits = DataBits.Eight,
            DtrEnable = false,              // ESP32 doesn't need DTR reset
            RtsEnable = false,
            MonitorPinChanges = false,
            ReadStrategy = ReadStrategy.AsyncWithSyncFallback
        };

        /// <summary>
        /// Raspberry Pi configuration (115200 baud)
        /// </summary>
        public static SerialPortOptions RaspberryPi(string portName) => new()
        {
            PortName = portName,
            BaudRate = (int)BaudRate.Baud115200,
            StopBits = StopBits.One,
            Parity = Parity.None,
            DataBits = DataBits.Eight,
            DtrEnable = false,
            RtsEnable = false,
            MonitorPinChanges = true,
            ReadStrategy = ReadStrategy.SyncOnly  // More reliable for Pi
        };

        /// <summary>
        /// GPS module configuration (9600 baud, typical for NMEA)
        /// </summary>
        public static SerialPortOptions GPS(string portName) => new()
        {
            PortName = portName,
            BaudRate = (int)BaudRate.Baud9600,
            StopBits = StopBits.One,
            Parity = Parity.None,
            DataBits = DataBits.Eight,
            DtrEnable = false,
            RtsEnable = false,
            MonitorPinChanges = false,
            ReadStrategy = ReadStrategy.SyncOnly,
            TimeoutMs = 2000
        };

        /// <summary>
        /// Bluetooth module configuration (38400 baud, common for HC-05/HC-06)
        /// </summary>
        public static SerialPortOptions Bluetooth(string portName) => new()
        {
            PortName = portName,
            BaudRate = (int)BaudRate.Baud38400,
            StopBits = StopBits.One,
            Parity = Parity.None,
            DataBits = DataBits.Eight,
            DtrEnable = false,
            RtsEnable = false,
            MonitorPinChanges = true,
            ReadStrategy = ReadStrategy.AsyncWithSyncFallback
        };

        /// <summary>
        /// Industrial RS485 configuration (9600 baud, RTS control)
        /// </summary>
        public static SerialPortOptions RS485(string portName) => new()
        {
            PortName = portName,
            BaudRate = (int)BaudRate.Baud9600,
            StopBits = StopBits.One,
            Parity = Parity.None,
            DataBits = DataBits.Eight,
            DtrEnable = false,
            RtsEnable = true,               // RTS controls transmit direction
            AutoSetRtsOnConnect = true,
            MonitorPinChanges = false,
            ReadStrategy = ReadStrategy.AsyncWithSyncFallback
        };

        /// <summary>
        /// High-speed industrial configuration (115200 baud)
        /// </summary>
        public static SerialPortOptions Industrial(string portName) => new()
        {
            PortName = portName,
            BaudRate = (int)BaudRate.Baud115200,
            StopBits = StopBits.One,
            Parity = Parity.None,
            DataBits = DataBits.Eight,
            DtrEnable = false,
            RtsEnable = false,
            MonitorPinChanges = true,
            ReadStrategy = ReadStrategy.SyncOnly,  // Maximum reliability
            TimeoutMs = 1000
        };

        /// <summary>
        /// Modem/AT command configuration (9600 baud with handshaking)
        /// </summary>
        public static SerialPortOptions Modem(string portName) => new()
        {
            PortName = portName,
            BaudRate = (int)BaudRate.Baud9600,
            StopBits = StopBits.One,
            Parity = Parity.None,
            DataBits = DataBits.Eight,
            DtrEnable = true,
            RtsEnable = true,
            AutoSetDtrOnConnect = true,
            AutoSetRtsOnConnect = true,
            MonitorPinChanges = true,
            PinChangeDebounceMs = 200,
            ReadStrategy = ReadStrategy.AsyncWithSyncFallback
        };

        /// <summary>
        /// Debug/Console configuration (115200 baud, reliable)
        /// </summary>
        public static SerialPortOptions Debug(string portName) => new()
        {
            PortName = portName,
            BaudRate = (int)BaudRate.Baud115200,
            StopBits = StopBits.One,
            Parity = Parity.None,
            DataBits = DataBits.Eight,
            DtrEnable = false,
            RtsEnable = false,
            MonitorPinChanges = false,
            ReadStrategy = ReadStrategy.SyncOnly,
            LogFallbackEvents = true,
            TimeoutMs = 10000  // Long timeout for debugging
        };
    }
}