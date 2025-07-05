using System.Collections.Concurrent;
using CheapSerial.Configuration;
using CheapSerial.Core.Models;
using CheapSerial.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapSerial.Implementation
{
    /// <summary>
    /// Service for managing multiple serial ports with async events
    /// </summary>
    public class SerialPortService : ISerialPortService
    {
        private readonly ISerialPortFactory _factory;
        private readonly IOptionsMonitor<SerialPortConfiguration> _config;
        private readonly ILogger<SerialPortService> _logger;
        private readonly ConcurrentDictionary<string, ISerialPortManager> _managers = new();
        private bool _disposed;

        // Event handling
        private readonly List<ConnectionStatusChangedHandler> _connectionStatusHandlers = new();
        private readonly List<SerialDataReceivedHandler> _dataReceivedHandlers = new();
        private readonly List<SerialPinChangedHandler> _pinChangedHandlers = new();

        public event ConnectionStatusChangedHandler ConnectionStatusChanged
        {
            add
            {
                lock (_connectionStatusHandlers)
                {
                    _connectionStatusHandlers.Add(value);
                }
            }
            remove
            {
                lock (_connectionStatusHandlers)
                {
                    _connectionStatusHandlers.Remove(value);
                }
            }
        }

        public event SerialDataReceivedHandler DataReceived
        {
            add
            {
                lock (_dataReceivedHandlers)
                {
                    _dataReceivedHandlers.Add(value);
                }
            }
            remove
            {
                lock (_dataReceivedHandlers)
                {
                    _dataReceivedHandlers.Remove(value);
                }
            }
        }

        public event SerialPinChangedHandler PinChanged
        {
            add
            {
                lock (_pinChangedHandlers)
                {
                    _pinChangedHandlers.Add(value);
                }
            }
            remove
            {
                lock (_pinChangedHandlers)
                {
                    _pinChangedHandlers.Remove(value);
                }
            }
        }

        public SerialPortService(
            ISerialPortFactory factory,
            IOptionsMonitor<SerialPortConfiguration> config,
            ILogger<SerialPortService> logger)
        {
            _factory = factory;
            _config = config;
            _logger = logger;

            // Initialize configured ports
            InitializeConfiguredPorts();
        }

        public IEnumerable<string> GetPortNames() => _managers.Keys;

        public async Task<bool> ConnectAsync(string portName)
        {
            if (string.IsNullOrEmpty(portName) || _disposed) return false;

            var manager = GetOrCreateManager(portName);
            return await manager.ConnectAsync();
        }

        public void Disconnect(string portName)
        {
            if (_managers.TryGetValue(portName, out var manager))
            {
                manager.Disconnect();
            }
        }

        public void DisconnectAll()
        {
            foreach (var manager in _managers.Values)
            {
                manager.Disconnect();
            }
        }

        public bool IsConnected(string portName) =>
            _managers.TryGetValue(portName, out var manager) && manager.IsConnected;

        public async Task<bool> SendAsync(string portName, byte[] data)
        {
            if (_managers.TryGetValue(portName, out var manager))
            {
                return await manager.SendAsync(data);
            }
            return false;
        }

        public async Task<bool> SendAsync(string portName, string text)
        {
            if (_managers.TryGetValue(portName, out var manager))
            {
                return await manager.SendAsync(text);
            }
            return false;
        }

        public ISerialPortManager GetManager(string portName) =>
            _managers.TryGetValue(portName, out var manager) ? manager : null;

        #region Voltage/Signal Control Methods

        public void SetDtr(string portName, bool enable)
        {
            if (_managers.TryGetValue(portName, out var manager))
            {
                manager.SetDtr(enable);
            }
        }

        public void SetRts(string portName, bool enable)
        {
            if (_managers.TryGetValue(portName, out var manager))
            {
                manager.SetRts(enable);
            }
        }

        public void SetBreakState(string portName, bool enable)
        {
            if (_managers.TryGetValue(portName, out var manager))
            {
                manager.SetBreakState(enable);
            }
        }

        public bool GetDtr(string portName)
        {
            if (_managers.TryGetValue(portName, out var manager))
            {
                return manager.GetDtr();
            }
            return false;
        }

        public bool GetRts(string portName)
        {
            if (_managers.TryGetValue(portName, out var manager))
            {
                return manager.GetRts();
            }
            return false;
        }

        public SerialPinStates GetPinStates(string portName)
        {
            if (_managers.TryGetValue(portName, out var manager))
            {
                return manager.GetPinStates();
            }
            return new SerialPinStates();
        }

        public void SetVoltageConfiguration(string portName, bool dtrEnable, bool rtsEnable, bool breakState = false)
        {
            if (_managers.TryGetValue(portName, out var manager))
            {
                manager.SetVoltageConfiguration(dtrEnable, rtsEnable, breakState);
            }
        }

        public string[] GetAvailableComPorts()
        {
            try
            {
                return System.IO.Ports.SerialPort.GetPortNames();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available COM ports");
                return Array.Empty<string>();
            }
        }

        #endregion

        private void InitializeConfiguredPorts()
        {
            var configuration = _config.CurrentValue;

            foreach (var portConfig in configuration.Ports)
            {
                try
                {
                    var manager = _factory.CreateManager(portConfig.Value);
                    _managers.TryAdd(portConfig.Key, manager);

                    manager.ConnectionStatusChanged += OnManagerConnectionStatusChanged;
                    manager.DataReceived += OnManagerDataReceived;
                    manager.PinChanged += OnManagerPinChanged;

                    _logger.LogInformation("Initialized serial port manager for {PortName}", portConfig.Key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize serial port {PortName}", portConfig.Key);
                }
            }
        }

        private ISerialPortManager GetOrCreateManager(string portName)
        {
            return _managers.GetOrAdd(portName, name =>
            {
                var options = _config.CurrentValue.Ports.TryGetValue(name, out var portConfig)
                    ? portConfig
                    : new SerialPortOptions { PortName = name, AutoConnect = false };

                var manager = _factory.CreateManager(options);

                manager.ConnectionStatusChanged += OnManagerConnectionStatusChanged;
                manager.DataReceived += OnManagerDataReceived;
                manager.PinChanged += OnManagerPinChanged;

                return manager;
            });
        }

        private async Task OnManagerConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            var handlers = new List<ConnectionStatusChangedHandler>();
            lock (_connectionStatusHandlers)
            {
                handlers.AddRange(_connectionStatusHandlers);
            }

            var tasks = handlers.Select(handler => handler(sender, e));
            await Task.WhenAll(tasks);
        }

        private async Task OnManagerDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var handlers = new List<SerialDataReceivedHandler>();
            lock (_dataReceivedHandlers)
            {
                handlers.AddRange(_dataReceivedHandlers);
            }

            var tasks = handlers.Select(handler => handler(sender, e));
            await Task.WhenAll(tasks);
        }

        private async Task OnManagerPinChanged(object sender, SerialPinChangedEventArgs e)
        {
            var handlers = new List<SerialPinChangedHandler>();
            lock (_pinChangedHandlers)
            {
                handlers.AddRange(_pinChangedHandlers);
            }

            var tasks = handlers.Select(handler => handler(sender, e));
            await Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var manager in _managers.Values)
            {
                manager.Dispose();
            }

            _managers.Clear();

            // Clear event handlers
            lock (_connectionStatusHandlers)
            {
                _connectionStatusHandlers.Clear();
            }
            lock (_dataReceivedHandlers)
            {
                _dataReceivedHandlers.Clear();
            }
            lock (_pinChangedHandlers)
            {
                _pinChangedHandlers.Clear();
            }

            _logger.LogDebug("SerialPortService disposed");
        }
    }
}