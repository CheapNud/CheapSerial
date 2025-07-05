
## 🔧 Usage Examples

### Program.cs (Console App)
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using CheapSerial;

var builder = Host.CreateApplicationBuilder(args);

// Add serial port services
builder.Services.AddSerialPortServices(builder.Configuration);

// Add your business services
builder.Services.AddScoped<DeviceCommunicationService>();

var host = builder.Build();

// Use the services
var serialService = host.Services.GetRequiredService<ISerialPortService>();
await serialService.ConnectAsync("Arduino");

await host.RunAsync();
```

### Program.cs (Blazor Server)
```csharp
using CheapSerial;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add serial port services
builder.Services.AddSerialPortServices(builder.Configuration);
builder.Services.AddScoped<DeviceCommunicationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "CheapSerial": "Debug"
    }
  },
  "SerialPorts": {
    "Ports": {
      "Arduino": {
        "PortName": "COM3",
        "BaudRate": 115200,
        "ReadStrategy": "AsyncWithSyncFallback",
        "DtrEnable": true,
        "AutoSetDtrOnConnect": true,
        "MonitorPinChanges": true,
        "AutoConnect": true
      },
      "ESP32": {
        "PortName": "COM4",
        "BaudRate": 115200,
        "ReadStrategy": "AsyncWithSyncFallback",
        "DtrEnable": false,
        "RtsEnable": false,
        "AutoConnect": true
      },
      "GPS": {
        "PortName": "COM6",
        "BaudRate": 9600,
        "ReadStrategy": "SyncOnly",
        "TimeoutMs": 2000,
        "AutoConnect": true
      }
    }
  }
}
```

### Blazor Component Example
```razor
@page "/serial"
@inject ISerialPortService SerialPortService
@implements IDisposable

<h3>Serial Port Communication</h3>

@foreach (var portName in SerialPortService.GetPortNames())
{
    <div class="mb-3">
        <MudCard>
            <MudCardContent>
                <MudText Typo="Typo.h6">@portName</MudText>
                <MudText>Status: @(SerialPortService.IsConnected(portName) ? "Connected" : "Disconnected")</MudText>
                <MudButton OnClick="@(() => ToggleConnection(portName))" 
                          Color="@(SerialPortService.IsConnected(portName) ? Color.Error : Color.Success)">
                    @(SerialPortService.IsConnected(portName) ? "Disconnect" : "Connect")
                </MudButton>
            </MudCardContent>
        </MudCard>
    </div>
}

@code {
    protected override async Task OnInitializedAsync()
    {
        SerialPortService.DataReceived += OnDataReceived;
        SerialPortService.ConnectionStatusChanged += OnConnectionChanged;
    }

    private async Task ToggleConnection(string portName)
    {
        if (SerialPortService.IsConnected(portName))
        {
            SerialPortService.Disconnect(portName);
        }
        else
        {
            await SerialPortService.ConnectAsync(portName);
        }
        StateHasChanged();
    }

    private async Task OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        await InvokeAsync(() =>
        {
            // Handle received data
            StateHasChanged();
        });
    }

    private async Task OnConnectionChanged(object sender, ConnectionStatusChangedEventArgs e)
    {
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        SerialPortService.DataReceived -= OnDataReceived;
        SerialPortService.ConnectionStatusChanged -= OnConnectionChanged;
    }
}
```