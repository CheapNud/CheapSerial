using CheapSerial.Configuration;
using CheapSerial.Core.Enums;
using CheapSerial.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace CheapSerial.Implementation
{
    /// <summary>
    /// Factory for creating SerialPortManager instances
    /// </summary>
    public class SerialPortFactory : ISerialPortFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SerialPortFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ISerialPortManager CreateManager(SerialPortOptions options)
        {
            var logger = _serviceProvider.GetService<ILogger<SerialPortManager>>();
            var manager = new SerialPortManager(logger);

            manager.SetPort(options.PortName, options.BaudRate, options.StopBits,
                options.Parity, options.DataBits);
            manager.TimeoutMs = options.TimeoutMs;
            manager.ReconnectDelayMs = options.ReconnectDelayMs;

            // Configure fallback strategy
            manager.SetReadStrategy(options.ReadStrategy, options.EnableSyncFallback, options.SyncFallbackTimeoutMs);

            // Configure voltage/signal settings
            manager.ConfigureVoltageSettings(options);

            if (options.AutoConnect)
            {
                Task.Run(manager.ConnectAsync);
            }

            return manager;
        }

        public ISerialPortManager CreateManager(string portName, int baudRate = (int)BaudRate.Baud115200,
            StopBits stopBits = StopBits.One, Parity parity = Parity.None, DataBits dataBits = DataBits.Eight)
        {
            var options = new SerialPortOptions
            {
                PortName = portName,
                BaudRate = baudRate,
                StopBits = stopBits,
                Parity = parity,
                DataBits = dataBits,
                AutoConnect = false
            };

            return CreateManager(options);
        }

        public ISerialPortManager CreateManager(string portName, BaudRate baudRate,
            StopBits stopBits = StopBits.One, Parity parity = Parity.None, DataBits dataBits = DataBits.Eight)
        {
            return CreateManager(portName, (int)baudRate, stopBits, parity, dataBits);
        }

        // Predefined configuration factory methods
        public ISerialPortManager CreateArduino(string portName) => CreateManager(SerialConfigurations.Arduino(portName));
        public ISerialPortManager CreateArduinoHighSpeed(string portName) => CreateManager(SerialConfigurations.ArduinoHighSpeed(portName));
        public ISerialPortManager CreateESP32(string portName) => CreateManager(SerialConfigurations.ESP32(portName));
        public ISerialPortManager CreateRaspberryPi(string portName) => CreateManager(SerialConfigurations.RaspberryPi(portName));
        public ISerialPortManager CreateGPS(string portName) => CreateManager(SerialConfigurations.GPS(portName));
        public ISerialPortManager CreateBluetooth(string portName) => CreateManager(SerialConfigurations.Bluetooth(portName));
        public ISerialPortManager CreateRS485(string portName) => CreateManager(SerialConfigurations.RS485(portName));
        public ISerialPortManager CreateIndustrial(string portName) => CreateManager(SerialConfigurations.Industrial(portName));
        public ISerialPortManager CreateModem(string portName) => CreateManager(SerialConfigurations.Modem(portName));
        public ISerialPortManager CreateDebug(string portName) => CreateManager(SerialConfigurations.Debug(portName));
    }
}