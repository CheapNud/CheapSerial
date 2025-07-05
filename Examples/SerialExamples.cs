using CheapSerial.Core.Enums;
using CheapSerial.Core.Models;
using CheapSerial.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CheapSerial.Examples
{
    /// <summary>
    /// Example configurations using type-safe enums and constants
    /// </summary>
    public static class TypeSafeSerialExamples
    {
        /// <summary>
        /// Using COM port constants and baud rate enums
        /// </summary>
        public static async Task BasicTypeSafeExample(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetRequiredService<ISerialPortFactory>();

            // Type-safe configuration using enums and constants
            var manager = factory.CreateManager(
                ComPorts.COM3,                    // Type-safe COM port
                BaudRate.Baud115200,              // Type-safe baud rate enum
                System.IO.Ports.StopBits.One,    // Already type-safe
                System.IO.Ports.Parity.None,     // Already type-safe  
                DataBits.Eight                    // Type-safe data bits enum
            );

            manager.DataReceived += OnDataReceivedAsync;
            await manager.ConnectAsync();

            // Voltage control with constants
            manager.SetVoltageConfiguration(
                dtrEnable: true,  // DTR provides SerialVoltages.DTR_HIGH_VOLTAGE (~12V)
                rtsEnable: false, // RTS at SerialVoltages.RTS_LOW_VOLTAGE (0V)
                breakState: false
            );

            Debug.WriteLine($"DTR Voltage: {(manager.GetDtr() ? SerialVoltages.DTR_HIGH_VOLTAGE : SerialVoltages.DTR_LOW_VOLTAGE)}V");
        }

        /// <summary>
        /// Using predefined device configurations
        /// </summary>
        public static async Task PredefinedConfigExample(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetRequiredService<ISerialPortFactory>();

            // Arduino on COM3 - perfect configuration out of the box
            var arduino = factory.CreateArduino(ComPorts.COM3);

            // ESP32 on COM4 - optimized for ESP32 behavior
            var esp32 = factory.CreateESP32(ComPorts.COM4);

            // Industrial device on COM5 - maximum reliability
            var industrial = factory.CreateIndustrial(ComPorts.COM5);

            // GPS module on COM6 - NMEA optimized
            var gps = factory.CreateGPS(ComPorts.COM6);

            // Connect all devices
            await arduino.ConnectAsync();
            await esp32.ConnectAsync();
            await industrial.ConnectAsync();
            await gps.ConnectAsync();

            Debug.WriteLine($"Arduino connected to {arduino.PortName}");
            Debug.WriteLine($"ESP32 connected to {esp32.PortName}");
            Debug.WriteLine($"Industrial device connected to {industrial.PortName}");
            Debug.WriteLine($"GPS connected to {gps.PortName}");
        }

        /// <summary>
        /// Cross-platform port discovery using constants
        /// </summary>
        public static async Task CrossPlatformExample(IServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetRequiredService<ISerialPortService>();
            var factory = serviceProvider.GetRequiredService<ISerialPortFactory>();

            // Windows COM ports
            var windowsPorts = new[]
            {
                ComPorts.COM1, ComPorts.COM3, ComPorts.COM4, ComPorts.COM5
            };

            // Linux USB ports  
            var linuxPorts = new[]
            {
                ComPorts.TTY_USB0, ComPorts.TTY_USB1, ComPorts.TTY_ACM0, ComPorts.TTY_ACM1
            };

            // Dynamic port generation
            var dynamicPorts = new[]
            {
                ComPorts.GetComPort(7),      // COM7
                ComPorts.GetComPort(8),      // COM8
                ComPorts.GetTtyUsb(2),       // /dev/ttyUSB2
                ComPorts.GetTtyAcm(3)        // /dev/ttyACM3
            };

            // Try to find available ports
            var availablePorts = service.GetAvailableComPorts();

            foreach (var port in availablePorts)
            {
                Debug.WriteLine($"🔍 Available port: {port}");

                // Test different baud rates
                await TestBaudRates(factory, port);
            }
        }

        /// <summary>
        /// Testing multiple baud rates using enum
        /// </summary>
        private static async Task TestBaudRates(ISerialPortFactory factory, string portName)
        {
            var commonBaudRates = new[]
            {
                BaudRate.Baud9600,
                BaudRate.Baud38400,
                BaudRate.Baud57600,
                BaudRate.Baud115200
            };

            foreach (var baudRate in commonBaudRates)
            {
                Debug.WriteLine($"   Testing {portName} at {baudRate} ({(int)baudRate} baud)");

                var manager = factory.CreateManager(portName, baudRate);

                if (await manager.ConnectAsync())
                {
                    Debug.WriteLine($"   ✅ {portName} responds at {baudRate}");

                    // Send test command
                    await manager.SendAsync("AT\r\n");
                    await Task.Delay(500);

                    manager.Disconnect();
                    manager.Dispose();
                    break;
                }
                else
                {
                    Debug.WriteLine($"   ❌ {portName} no response at {baudRate}");
                    manager.Dispose();
                }
            }
        }

        /// <summary>
        /// Voltage monitoring example with constants
        /// </summary>
        public static async Task VoltageMonitoringExample(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetRequiredService<ISerialPortFactory>();

            // Create device with voltage monitoring
            var device = factory.CreateManager(ComPorts.COM3, BaudRate.Baud9600);
            device.PinChanged += OnVoltageChangedAsync;

            await device.ConnectAsync();

            // Set voltages and monitor
            device.SetDtr(true);  // Apply ~12V
            Debug.WriteLine($"DTR set to {SerialVoltages.DTR_HIGH_VOLTAGE}V");

            device.SetRts(false); // Apply 0V
            Debug.WriteLine($"RTS set to {SerialVoltages.RTS_LOW_VOLTAGE}V");

            // Monitor pin states
            var pinStates = device.GetPinStates();
            Debug.WriteLine($"Input voltages - CTS: {(pinStates.CtsHolding ? "HIGH" : "LOW")}, " +
                           $"DSR: {(pinStates.DsrHolding ? "HIGH" : "LOW")}, " +
                           $"CD: {(pinStates.CDHolding ? "HIGH" : "LOW")}");
        }

        /// <summary>
        /// Service-level example with multiple devices
        /// </summary>
        public static async Task ServiceLevelExample(IServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetRequiredService<ISerialPortService>();

            // Subscribe to service-level events
            service.ConnectionStatusChanged += OnConnectionChangedAsync;
            service.DataReceived += OnDataReceivedAsync;
            service.PinChanged += OnPinChangedAsync;

            // Connect to multiple devices (configured in appsettings.json)
            await service.ConnectAsync("Arduino");
            await service.ConnectAsync("ESP32");
            await service.ConnectAsync("GPS");

            // Send commands to specific devices
            await service.SendAsync("Arduino", "GET_SENSOR_DATA\r\n");
            await service.SendAsync("ESP32", "GET_WIFI_STATUS\r\n");
            await service.SendAsync("GPS", "GET_POSITION\r\n");

            // Control voltages on specific devices
            service.SetDtr("Arduino", true);    // Power Arduino via DTR
            service.SetRts("ESP32", false);     // Control ESP32 boot mode

            // Monitor all connected devices
            foreach (var portName in service.GetPortNames())
            {
                var isConnected = service.IsConnected(portName);
                var pinStates = service.GetPinStates(portName);

                Debug.WriteLine($"Device {portName}: Connected={isConnected}, " +
                               $"CTS={pinStates.CtsHolding}, DSR={pinStates.DsrHolding}");
            }
        }

        #region Event Handlers

        private static async Task OnDataReceivedAsync(object sender, SerialDataReceivedEventArgs e)
        {
            var text = System.Text.Encoding.UTF8.GetString(e.Data);
            Debug.WriteLine($"📥 {e.PortName}: {text.Trim()}");
        }

        private static async Task OnConnectionChangedAsync(object sender, ConnectionStatusChangedEventArgs e)
        {
            Debug.WriteLine($"🔌 {e.PortName}: {(e.IsConnected ? "Connected" : "Disconnected")}");
        }

        private static async Task OnPinChangedAsync(object sender, SerialPinChangedEventArgs e)
        {
            Debug.WriteLine($"📌 Pin change on {e.PortName}: {e.EventType}");
        }

        private static async Task OnVoltageChangedAsync(object sender, SerialPinChangedEventArgs e)
        {
            Debug.WriteLine($"⚡ Voltage change on {e.PortName}: {e.EventType}");
            Debug.WriteLine($"   CTS: {(e.CurrentPinStates.CtsHolding ? $"{SerialVoltages.TTL_HIGH_VOLTAGE}V" : $"{SerialVoltages.TTL_LOW_VOLTAGE}V")}");
            Debug.WriteLine($"   DSR: {(e.CurrentPinStates.DsrHolding ? $"{SerialVoltages.TTL_HIGH_VOLTAGE}V" : $"{SerialVoltages.TTL_LOW_VOLTAGE}V")}");
        }

        #endregion
    }

    /// <summary>
    /// Example startup configuration for a real application
    /// </summary>
    public static class StartupExample
    {
        /// <summary>
        /// Example Program.cs configuration
        /// </summary>
        public static void ConfigureServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            // Add logging (requires Microsoft.Extensions.Logging.Console package)
            services.AddLogging(builder =>
            {
                builder.AddConsole();    // Requires: Microsoft.Extensions.Logging.Console
                builder.AddDebug();      // Requires: Microsoft.Extensions.Logging.Debug
            });

            // Add serial port services with configuration
            services.AddSerialPortServices(configuration);

            // Or configure programmatically:
            /*
            services.AddSerialPortServices(config =>
            {
                config.Ports["Arduino"] = SerialConfigurations.Arduino(ComPorts.COM3);
                config.Ports["ESP32"] = SerialConfigurations.ESP32(ComPorts.COM4);
                config.Ports["GPS"] = SerialConfigurations.GPS(ComPorts.COM6);
            });
            */

            // Register your business services
            services.AddScoped<DeviceCommunicationService>();
        }

        /// <summary>
        /// Example appsettings.json structure
        /// </summary>
        public static string GetExampleAppsettingsJson()
        {
            return @"{
  ""SerialPorts"": {
    ""Ports"": {
      ""Arduino"": {
        ""PortName"": ""COM3"",
        ""BaudRate"": 115200,
        ""ReadStrategy"": ""AsyncWithSyncFallback"",
        ""DtrEnable"": true,
        ""AutoSetDtrOnConnect"": true,
        ""MonitorPinChanges"": true,
        ""AutoConnect"": true
      },
      ""ESP32"": {
        ""PortName"": ""COM4"",
        ""BaudRate"": 115200,
        ""ReadStrategy"": ""AsyncWithSyncFallback"",
        ""DtrEnable"": false,
        ""RtsEnable"": false,
        ""AutoConnect"": true
      },
      ""GPS"": {
        ""PortName"": ""COM6"",
        ""BaudRate"": 9600,
        ""ReadStrategy"": ""SyncOnly"",
        ""TimeoutMs"": 2000,
        ""AutoConnect"": true
      }
    }
  }
}";
        }
    }

    /// <summary>
    /// Example business service using the serial port services
    /// </summary>
    public class DeviceCommunicationService
    {
        private readonly ISerialPortService _serialPortService;
        private readonly ILogger<DeviceCommunicationService> _logger;

        public DeviceCommunicationService(ISerialPortService serialPortService, ILogger<DeviceCommunicationService> logger)
        {
            _serialPortService = serialPortService;
            _logger = logger;

            // Subscribe to events
            _serialPortService.DataReceived += OnDataReceivedAsync;
            _serialPortService.ConnectionStatusChanged += OnConnectionStatusChangedAsync;
        }

        public async Task<bool> SendCommandAsync(string deviceName, string command)
        {
            _logger.LogDebug("Sending command to {DeviceName}: {Command}", deviceName, command);
            return await _serialPortService.SendAsync(deviceName, command + "\r\n");
        }

        private async Task OnDataReceivedAsync(object sender, SerialDataReceivedEventArgs e)
        {
            var message = System.Text.Encoding.UTF8.GetString(e.Data);
            _logger.LogInformation("Device {PortName} sent: {Message}", e.PortName, message.Trim());
        }

        private async Task OnConnectionStatusChangedAsync(object sender, ConnectionStatusChangedEventArgs e)
        {
            _logger.LogInformation("Device {PortName} {Status}",
                e.PortName, e.IsConnected ? "connected" : "disconnected");
        }
    }
}