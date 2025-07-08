using CheapSerial.Configuration;
using CheapSerial.Core.Enums;
using CheapSerial.Core.Models;
using CheapSerial.Interfaces;
using Microsoft.Extensions.Logging;

using System.Runtime.InteropServices;
using static CheapSerial.Core.Models.ConnectionStatusChangedEventArgs;
using static CheapSerial.Core.Models.SerialDataReceivedEventArgs;
using static CheapSerial.Core.Models.SerialPinChangedEventArgs;

namespace CheapSerial
{
    /// <summary>
    /// Complete SerialPortManager implementation with all private methods
    /// </summary>
    public class SerialPortManager : ISerialPortManager
    {
        #region Private Fields

        private readonly ILogger<SerialPortManager> _logger;
        private System.IO.Ports.SerialPort _serialPort;
        private string _portName = "";
        private int _baudRate = (int)BaudRate.Baud115200;
        private System.IO.Ports.StopBits _stopBits = System.IO.Ports.StopBits.One;
        private System.IO.Ports.Parity _parity = System.IO.Ports.Parity.None;
        private DataBits _dataBits = DataBits.Eight;

        private bool _hasReadWriteError;
        private bool _disconnectRequested;
        private bool _disposed;

        private Thread _readerThread;
        private CancellationTokenSource _readerCancellation;
        private readonly byte[] _readBuffer = new byte[4096];

        private Timer _connectionWatcher;
        private readonly object _serialPortLock = new object();

        // Event handling
        private readonly List<ConnectionStatusChangedHandler> _connectionStatusHandlers = new();
        private readonly List<SerialDataReceivedHandler> _dataReceivedHandlers = new();
        private readonly List<SerialPinChangedHandler> _pinChangedHandlers = new();
        private readonly SemaphoreSlim _eventSemaphore = new(1, 1);

        // Fallback strategy configuration
        private ReadStrategy _readStrategy = ReadStrategy.AsyncWithSyncFallback;
        private bool _enableSyncFallback = true;
        private int _syncFallbackTimeoutMs = 100;
        private int _asyncRetryCount = 1;
        private bool _logFallbackEvents = true;
        private int _successfulReadsBeforeAsyncPromotion = 10;

        // Voltage/Signal control configuration
        private bool _dtrEnable = false;
        private bool _rtsEnable = false;
        private bool _breakState = false;
        private bool _autoSetDtrOnConnect = false;
        private bool _autoSetRtsOnConnect = false;
        private bool _monitorPinChanges = false;
        private int _pinChangeDebounceMs = 50;
        private SerialPinStates _lastPinStates = new();
        private DateTime _lastPinChangeTime = DateTime.MinValue;

        // Runtime strategy tracking
        private bool _currentlyUsingSyncReads = false;
        private int _consecutiveSuccessfulReads = 0;
        private int _fallbackEventCount = 0;
        private DateTime _lastFallbackEvent = DateTime.MinValue;

        private const int ReaderJoinTimeoutMs = 5000;
        private const int ConnectionWatcherIntervalMs = 1000;
        private const int DefaultReconnectDelayMs = 1000;
        private const int DefaultTimeoutMs = 5000;

        #endregion

        #region Public Events

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

        #endregion

        #region Public Properties

        public string PortName => _portName;

        public bool IsConnected =>
            _serialPort?.IsOpen == true &&
            !_hasReadWriteError &&
            !_disconnectRequested &&
            !_disposed;

        public int ReconnectDelayMs { get; set; } = DefaultReconnectDelayMs;
        public int TimeoutMs { get; set; } = DefaultTimeoutMs;
        public System.IO.Ports.SerialPort SerialPort => _serialPort;

        #endregion

        #region Constructor

        public SerialPortManager(ILogger<SerialPortManager> logger = null)
        {
            _logger = logger;
            _readerCancellation = new CancellationTokenSource();
        }

        #endregion

        #region Public Methods

        public void SetPort(string portName, int baudRate = (int)BaudRate.Baud115200,
            System.IO.Ports.StopBits stopBits = System.IO.Ports.StopBits.One, System.IO.Ports.Parity parity = System.IO.Ports.Parity.None,
            DataBits dataBits = DataBits.Eight)
        {
            if (_portName != portName || _baudRate != baudRate ||
                _stopBits != stopBits || _parity != parity || _dataBits != dataBits)
            {
                _portName = portName;
                _baudRate = baudRate;
                _stopBits = stopBits;
                _parity = parity;
                _dataBits = dataBits;

                _logger?.LogDebug("Port configuration changed: {PortName} @ {BaudRate} baud, " +
                    "{DataBits} data bits, {StopBits} stop bits, {Parity} parity",
                    _portName, _baudRate, _dataBits, _stopBits, _parity);

                if (IsConnected)
                {
                    Task.Run(ConnectAsync);
                }
            }
        }

        public void SetPort(string portName, BaudRate baudRate, System.IO.Ports.StopBits stopBits = System.IO.Ports.StopBits.One,
            System.IO.Ports.Parity parity = System.IO.Ports.Parity.None, DataBits dataBits = DataBits.Eight)
        {
            SetPort(portName, (int)baudRate, stopBits, parity, dataBits);
        }

        public void SetReadStrategy(ReadStrategy strategy, bool enableSyncFallback = true, int syncFallbackTimeoutMs = 100)
        {
            _readStrategy = strategy;
            _enableSyncFallback = enableSyncFallback;
            _syncFallbackTimeoutMs = syncFallbackTimeoutMs;

            // Reset strategy tracking when strategy changes
            _currentlyUsingSyncReads = (strategy == ReadStrategy.SyncOnly || strategy == ReadStrategy.SyncWithAsyncPromotion);
            _consecutiveSuccessfulReads = 0;
            _fallbackEventCount = 0;

            _logger?.LogDebug("Read strategy changed for {PortName}: {Strategy}, Fallback: {EnableFallback}, Timeout: {TimeoutMs}ms",
                _portName, strategy, enableSyncFallback, syncFallbackTimeoutMs);
        }

        public async Task<bool> ConnectAsync()
        {
            if (_disconnectRequested || _disposed)
                return false;

            await Task.Run(() =>
            {
                lock (_serialPortLock)
                {
                    DisconnectInternal();
                    OpenPort();
                    StartConnectionWatcher();
                }
            });

            return IsConnected;
        }

        public void Disconnect()
        {
            if (_disposed) return;

            _disconnectRequested = true;

            lock (_serialPortLock)
            {
                StopConnectionWatcher();
                DisconnectInternal();
                _disconnectRequested = false;
            }
        }

        public async Task<bool> SendAsync(byte[] data)
        {
            if (!IsConnected || data == null || data.Length == 0)
                return false;

            try
            {
                await Task.Run(() =>
                {
                    _serialPort.Write(data, 0, data.Length);
                });

                _logger?.LogDebug("Sent {Count} bytes to {PortName}: {Data}",
                    data.Length, _portName, Convert.ToHexString(data));
                return true;
            }
            catch (ObjectDisposedException ex)
            {
                _logger?.LogWarning("Send failed - port disposed: {Message}", ex.Message);
                _hasReadWriteError = true;
                return false;
            }
            catch (TimeoutException ex)
            {
                _logger?.LogWarning("Send timeout on {PortName}: {Message}", _portName, ex.Message);
                _hasReadWriteError = true;
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Send error on {PortName}", _portName);
                return false;
            }
        }

        public Task<bool> SendAsync(string text) =>
            SendAsync(System.Text.Encoding.UTF8.GetBytes(text));

        #endregion

        #region Voltage/Signal Control Methods

        public void SetDtr(bool enable)
        {
            if (!IsConnected)
            {
                _logger?.LogWarning("Cannot set DTR on {PortName} - port not connected", _portName);
                return;
            }

            try
            {
                _serialPort.DtrEnable = enable;
                _dtrEnable = enable;
                _logger?.LogDebug("DTR set to {State} on {PortName}", enable ? "HIGH (~12V)" : "LOW (0V)", _portName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to set DTR on {PortName}", _portName);
                throw;
            }
        }

        public void SetRts(bool enable)
        {
            if (!IsConnected)
            {
                _logger?.LogWarning("Cannot set RTS on {PortName} - port not connected", _portName);
                return;
            }

            try
            {
                _serialPort.RtsEnable = enable;
                _rtsEnable = enable;
                _logger?.LogDebug("RTS set to {State} on {PortName}", enable ? "HIGH (~12V)" : "LOW (0V)", _portName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to set RTS on {PortName}", _portName);
                throw;
            }
        }

        public void SetBreakState(bool enable)
        {
            if (!IsConnected)
            {
                _logger?.LogWarning("Cannot set break state on {PortName} - port not connected", _portName);
                return;
            }

            try
            {
                _serialPort.BreakState = enable;
                _breakState = enable;
                _logger?.LogDebug("Break state set to {State} on {PortName}", enable, _portName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to set break state on {PortName}", _portName);
                throw;
            }
        }

        public bool GetDtr()
        {
            if (!IsConnected) return _dtrEnable;
            try
            {
                return _serialPort.DtrEnable;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get DTR state on {PortName}", _portName);
                return _dtrEnable;
            }
        }

        public bool GetRts()
        {
            if (!IsConnected) return _rtsEnable;
            try
            {
                return _serialPort.RtsEnable;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get RTS state on {PortName}", _portName);
                return _rtsEnable;
            }
        }

        public bool GetBreakState()
        {
            if (!IsConnected) return _breakState;
            try
            {
                return _serialPort.BreakState;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get break state on {PortName}", _portName);
                return _breakState;
            }
        }

        public SerialPinStates GetPinStates()
        {
            if (!IsConnected)
            {
                return new SerialPinStates();
            }

            try
            {
                return new SerialPinStates
                {
                    CtsHolding = _serialPort.CtsHolding,
                    DsrHolding = _serialPort.DsrHolding,
                    CDHolding = _serialPort.CDHolding,
                    RingIndicator = false,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get pin states on {PortName}", _portName);
                return new SerialPinStates();
            }
        }

        public void SetVoltageConfiguration(bool dtrEnable, bool rtsEnable, bool breakState = false)
        {
            if (!IsConnected)
            {
                _dtrEnable = dtrEnable;
                _rtsEnable = rtsEnable;
                _breakState = breakState;
                _logger?.LogDebug("Voltage configuration saved for {PortName} (will apply on connect): DTR={DTR}, RTS={RTS}, Break={Break}",
                    _portName, dtrEnable, rtsEnable, breakState);
                return;
            }

            try
            {
                _serialPort.DtrEnable = dtrEnable;
                _serialPort.RtsEnable = rtsEnable;
                _serialPort.BreakState = breakState;

                _dtrEnable = dtrEnable;
                _rtsEnable = rtsEnable;
                _breakState = breakState;

                _logger?.LogInformation("Voltage configuration applied to {PortName}: DTR={DTR} ({DTRVoltage}), RTS={RTS} ({RTSVoltage}), Break={Break}",
                    _portName, dtrEnable, dtrEnable ? "~12V" : "0V", rtsEnable, rtsEnable ? "~12V" : "0V", breakState);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to set voltage configuration on {PortName}", _portName);
                throw;
            }
        }

        public int[] GetSupportedBaudRates()
        {
            return new int[]
            {
                110, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800,
                38400, 56000, 57600, 115200, 128000, 230400, 256000, 460800,
                921600, 1000000, 1152000, 1500000, 2000000, 2500000, 3000000
            };
        }

        public void ConfigureVoltageSettings(SerialPortOptions options)
        {
            _dtrEnable = options.DtrEnable;
            _rtsEnable = options.RtsEnable;
            _breakState = options.InitialBreakState;
            _autoSetDtrOnConnect = options.AutoSetDtrOnConnect;
            _autoSetRtsOnConnect = options.AutoSetRtsOnConnect;
            _monitorPinChanges = options.MonitorPinChanges;
            _pinChangeDebounceMs = options.PinChangeDebounceMs;

            _logger?.LogDebug("Voltage settings configured for {PortName}: DTR={DTR}, RTS={RTS}, Break={Break}, AutoDTR={AutoDTR}, AutoRTS={AutoRTS}, Monitor={Monitor}",
                _portName, _dtrEnable, _rtsEnable, _breakState, _autoSetDtrOnConnect, _autoSetRtsOnConnect, _monitorPinChanges);
        }

        #endregion

        #region Private Methods

        private bool OpenPort()
        {
            try
            {
                ClosePort();

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (!File.Exists(_portName))
                    {
                        _logger?.LogWarning("Port file does not exist: {PortName}", _portName);
                        return false;
                    }
                }

                _serialPort = new System.IO.Ports.SerialPort
                {
                    PortName = _portName,
                    BaudRate = _baudRate,
                    StopBits = _stopBits,
                    Parity = _parity,
                    DataBits = (int)_dataBits,
                    ReadTimeout = TimeoutMs,
                    WriteTimeout = TimeoutMs
                };

                _serialPort.ErrorReceived += OnSerialPortError;
                if (_monitorPinChanges)
                {
                    _serialPort.PinChanged += OnSerialPortPinChanged;
                }
                _serialPort.Open();

                _logger?.LogInformation("Serial port opened: {PortName}", _portName);

                ApplyVoltageSettings();
                _hasReadWriteError = false;
                StartReaderThread();

                if (_monitorPinChanges)
                {
                    StartPinMonitoring();
                }

                _ = OnConnectionStatusChangedAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open port {PortName}", _portName);
                ClosePort();
                return false;
            }
        }

        private void ClosePort()
        {
            StopReaderThread();

            if (_serialPort != null)
            {
                try
                {
                    _serialPort.ErrorReceived -= OnSerialPortError;
                    if (_monitorPinChanges)
                    {
                        _serialPort.PinChanged -= OnSerialPortPinChanged;
                    }

                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        _logger?.LogInformation("Serial port closed: {PortName}", _portName);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error closing port {PortName}", _portName);
                }
                finally
                {
                    _serialPort.Dispose();
                    _serialPort = null;
                    _ = OnConnectionStatusChangedAsync(false);
                }
            }

            _hasReadWriteError = true;
        }

        private void StartReaderThread()
        {
            StopReaderThread();

            _readerCancellation = new CancellationTokenSource();
            _readerThread = new Thread(ReaderThreadProc)
            {
                IsBackground = true,
                Name = $"SerialReader-{_portName}"
            };
            _readerThread.Start(_readerCancellation.Token);
        }

        private void StopReaderThread()
        {
            if (_readerThread != null)
            {
                _readerCancellation?.Cancel();

                if (!_readerThread.Join(ReaderJoinTimeoutMs))
                {
                    _logger?.LogWarning("Reader thread for {PortName} did not exit gracefully", _portName);
                }

                _readerThread = null;
            }
        }

        private void ReaderThreadProc(object parameter)
        {
            var cancellationToken = (CancellationToken)parameter;
            _logger?.LogDebug("Reader thread started for {PortName}", _portName);

            while (IsConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var readTask = ReadWithFallbackAsync(cancellationToken);
                    var bytesRead = readTask.GetAwaiter().GetResult();

                    if (bytesRead > 0)
                    {
                        var receivedData = new byte[bytesRead];
                        Buffer.BlockCopy(_readBuffer, 0, receivedData, 0, bytesRead);
                        _ = OnDataReceivedAsync(receivedData);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogDebug("Reader thread cancelled for {PortName}", _portName);
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Reader thread error for {PortName}", _portName);
                    _hasReadWriteError = true;

                    try
                    {
                        Thread.Sleep(DefaultReconnectDelayMs);
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                }
            }

            _logger?.LogDebug("Reader thread stopped for {PortName}", _portName);
        }

        private async Task<int> ReadWithFallbackAsync(CancellationToken cancellationToken)
        {
            switch (_readStrategy)
            {
                case ReadStrategy.AsyncOnly:
                    return await ReadAsyncOnly(cancellationToken);
                case ReadStrategy.SyncOnly:
                    return await ReadSyncOnly(cancellationToken);
                case ReadStrategy.AsyncWithSyncFallback:
                    return await ReadAsyncWithSyncFallback(cancellationToken);
                case ReadStrategy.SyncWithAsyncPromotion:
                    return await ReadSyncWithAsyncPromotion(cancellationToken);
                default:
                    return await ReadAsyncWithSyncFallback(cancellationToken);
            }
        }

        private async Task<int> ReadAsyncOnly(CancellationToken cancellationToken)
        {
            try
            {
                var bytesRead = await _serialPort.BaseStream.ReadAsync(_readBuffer, 0, _readBuffer.Length, cancellationToken);
                _consecutiveSuccessfulReads++;
                return bytesRead;
            }
            catch (IOException ex) when (ex.Message.Contains("OperationAborted") || ex.Message.Contains("Operation was canceled"))
            {
                if (_logFallbackEvents)
                {
                    _logger?.LogWarning("SerialStream async bug detected on {PortName} - AsyncOnly mode cannot fallback. Error: {Error}",
                        _portName, ex.Message);
                }
                _fallbackEventCount++;
                _lastFallbackEvent = DateTime.UtcNow;
                throw;
            }
        }

        private async Task<int> ReadSyncOnly(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var originalTimeout = _serialPort.ReadTimeout;
                    _serialPort.ReadTimeout = _syncFallbackTimeoutMs;

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var bytesRead = _serialPort.Read(_readBuffer, 0, _readBuffer.Length);
                        _consecutiveSuccessfulReads++;
                        return bytesRead;
                    }
                    finally
                    {
                        _serialPort.ReadTimeout = originalTimeout;
                    }
                }
                catch (TimeoutException)
                {
                    return 0;
                }
            }, cancellationToken);
        }

        private async Task<int> ReadAsyncWithSyncFallback(CancellationToken cancellationToken)
        {
            if (_currentlyUsingSyncReads)
            {
                return await ReadSyncFallback(cancellationToken);
            }

            for (int attempt = 0; attempt < _asyncRetryCount; attempt++)
            {
                try
                {
                    var bytesRead = await _serialPort.BaseStream.ReadAsync(_readBuffer, 0, _readBuffer.Length, cancellationToken);
                    _consecutiveSuccessfulReads++;
                    return bytesRead;
                }
                catch (IOException ex) when (ex.Message.Contains("OperationAborted") || ex.Message.Contains("Operation was canceled"))
                {
                    if (_logFallbackEvents)
                    {
                        _logger?.LogDebug("SerialStream async bug detected on {PortName} (attempt {Attempt}/{Total}), falling back to sync read",
                            _portName, attempt + 1, _asyncRetryCount);
                    }

                    _fallbackEventCount++;
                    _lastFallbackEvent = DateTime.UtcNow;

                    if (_enableSyncFallback)
                    {
                        _currentlyUsingSyncReads = true;
                        return await ReadSyncFallback(cancellationToken);
                    }
                    else if (attempt == _asyncRetryCount - 1)
                    {
                        throw;
                    }
                }
            }

            return 0;
        }

        private async Task<int> ReadSyncWithAsyncPromotion(CancellationToken cancellationToken)
        {
            if (!_currentlyUsingSyncReads || _consecutiveSuccessfulReads < _successfulReadsBeforeAsyncPromotion)
            {
                var bytesRead = await ReadSyncFallback(cancellationToken);

                if (_consecutiveSuccessfulReads >= _successfulReadsBeforeAsyncPromotion)
                {
                    if (_logFallbackEvents)
                    {
                        _logger?.LogInformation("Promoting {PortName} to async reads after {Count} successful sync reads",
                            _portName, _consecutiveSuccessfulReads);
                    }
                    _currentlyUsingSyncReads = false;
                }

                return bytesRead;
            }

            try
            {
                var bytesRead = await _serialPort.BaseStream.ReadAsync(_readBuffer, 0, _readBuffer.Length, cancellationToken);
                _consecutiveSuccessfulReads++;
                return bytesRead;
            }
            catch (IOException ex) when (ex.Message.Contains("OperationAborted") || ex.Message.Contains("Operation was canceled"))
            {
                if (_logFallbackEvents)
                {
                    _logger?.LogWarning("SerialStream async bug after promotion on {PortName}, reverting to sync reads",
                        _portName);
                }

                _fallbackEventCount++;
                _lastFallbackEvent = DateTime.UtcNow;
                _currentlyUsingSyncReads = true;
                _consecutiveSuccessfulReads = 0;

                return await ReadSyncFallback(cancellationToken);
            }
        }

        private async Task<int> ReadSyncFallback(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var originalTimeout = _serialPort.ReadTimeout;
                    _serialPort.ReadTimeout = _syncFallbackTimeoutMs;

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var bytesRead = _serialPort.Read(_readBuffer, 0, _readBuffer.Length);
                        _consecutiveSuccessfulReads++;

                        if (_logFallbackEvents && _consecutiveSuccessfulReads % 100 == 0)
                        {
                            _logger?.LogDebug("Sync fallback working well for {PortName} - {Count} consecutive successful reads",
                                _portName, _consecutiveSuccessfulReads);
                        }

                        return bytesRead;
                    }
                    finally
                    {
                        _serialPort.ReadTimeout = originalTimeout;
                    }
                }
                catch (TimeoutException)
                {
                    return 0;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in sync fallback read for {PortName}", _portName);
                    throw;
                }
            }, cancellationToken);
        }

        private void StartConnectionWatcher()
        {
            StopConnectionWatcher();
            _connectionWatcher = new Timer(ConnectionWatcherCallback, null,
                ConnectionWatcherIntervalMs, ConnectionWatcherIntervalMs);
        }

        private void StopConnectionWatcher()
        {
            _connectionWatcher?.Dispose();
            _connectionWatcher = null;
        }

        private void ConnectionWatcherCallback(object state)
        {
            if (_disconnectRequested || _disposed) return;

            if (_hasReadWriteError)
            {
                try
                {
                    if (_serialPort?.IsOpen == true)
                    {
                        _logger?.LogDebug("Connection watcher detected error, closing port {PortName}", _portName);
                        ClosePort();
                    }
                    else
                    {
                        _logger?.LogDebug("Connection watcher attempting reconnect to {PortName}", _portName);
                        OpenPort();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Connection watcher error for {PortName}", _portName);
                }
            }
        }

        private void DisconnectInternal()
        {
            StopConnectionWatcher();
            ClosePort();
        }

        private void OnSerialPortError(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
        {
            _logger?.LogWarning("Serial port error on {PortName}: {EventType}", _portName, e.EventType);
            _hasReadWriteError = true;
        }

        private void OnSerialPortPinChanged(object sender, System.IO.Ports.SerialPinChangedEventArgs e)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastPinChangeTime).TotalMilliseconds < _pinChangeDebounceMs)
            {
                return;
            }
            _lastPinChangeTime = now;

            try
            {
                var currentPinStates = GetPinStates();
                var eventArgs = new SerialPinChangedEventArgs(_portName, e.EventType, currentPinStates);

                _logger?.LogDebug("Pin change detected on {PortName}: {EventType}, CTS={CTS}, DSR={DSR}, CD={CD}",
                    _portName, e.EventType, currentPinStates.CtsHolding, currentPinStates.DsrHolding, currentPinStates.CDHolding);

                _ = OnPinChangedAsync(eventArgs);
                _lastPinStates = currentPinStates;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling pin change event on {PortName}", _portName);
            }
        }

        private void ApplyVoltageSettings()
        {
            try
            {
                if (_autoSetDtrOnConnect || _dtrEnable)
                {
                    _serialPort.DtrEnable = _dtrEnable;
                    _logger?.LogDebug("Applied DTR setting: {State} on {PortName}", _dtrEnable ? "HIGH (~12V)" : "LOW (0V)", _portName);
                }

                if (_autoSetRtsOnConnect || _rtsEnable)
                {
                    _serialPort.RtsEnable = _rtsEnable;
                    _logger?.LogDebug("Applied RTS setting: {State} on {PortName}", _rtsEnable ? "HIGH (~12V)" : "LOW (0V)", _portName);
                }

                if (_breakState)
                {
                    _serialPort.BreakState = _breakState;
                    _logger?.LogDebug("Applied break state: {State} on {PortName}", _breakState, _portName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to apply voltage settings on {PortName}", _portName);
            }
        }

        private void StartPinMonitoring()
        {
            try
            {
                _lastPinStates = GetPinStates();
                _logger?.LogDebug("Started pin monitoring on {PortName}: CTS={CTS}, DSR={DSR}, CD={CD}",
                    _portName, _lastPinStates.CtsHolding, _lastPinStates.DsrHolding, _lastPinStates.CDHolding);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start pin monitoring on {PortName}", _portName);
            }
        }

        private async Task OnConnectionStatusChangedAsync(bool isConnected)
        {
            _logger?.LogDebug("Connection status changed for {PortName}: {IsConnected}", _portName, isConnected);

            var eventArgs = new ConnectionStatusChangedEventArgs(_portName, isConnected);
            var handlers = new List<ConnectionStatusChangedHandler>();

            lock (_connectionStatusHandlers)
            {
                handlers.AddRange(_connectionStatusHandlers);
            }

            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                try
                {
                    tasks.Add(handler(this, eventArgs));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in connection status event handler for {PortName}", _portName);
                }
            }

            if (tasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error waiting for connection status event handlers to complete for {PortName}", _portName);
                }
            }
        }

        private async Task OnDataReceivedAsync(byte[] data)
        {
            _logger?.LogDebug("Received {Count} bytes from {PortName}: {Data}",
                data.Length, _portName, Convert.ToHexString(data));

            var eventArgs = new SerialDataReceivedEventArgs(_portName, data);
            var handlers = new List<SerialDataReceivedHandler>();

            lock (_dataReceivedHandlers)
            {
                handlers.AddRange(_dataReceivedHandlers);
            }

            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                try
                {
                    tasks.Add(handler(this, eventArgs));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in data received event handler for {PortName}", _portName);
                }
            }

            if (tasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error waiting for data received event handlers to complete for {PortName}", _portName);
                }
            }
        }

        private async Task OnPinChangedAsync(SerialPinChangedEventArgs eventArgs)
        {
            _logger?.LogDebug("Pin change event on {PortName}: {EventType}",
                _portName, eventArgs.EventType);

            var handlers = new List<SerialPinChangedHandler>();

            lock (_pinChangedHandlers)
            {
                handlers.AddRange(_pinChangedHandlers);
            }

            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                try
                {
                    tasks.Add(handler(this, eventArgs));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in pin changed event handler for {PortName}", _portName);
                }
            }

            if (tasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error waiting for pin changed event handlers to complete for {PortName}", _portName);
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            Disconnect();

            _readerCancellation?.Dispose();
            _eventSemaphore?.Dispose();

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

            _logger?.LogDebug("SerialPortManager for {PortName} disposed", _portName);
        }

        #endregion
    }
}