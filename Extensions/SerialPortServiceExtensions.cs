using CheapSerial.Configuration;
using CheapSerial.Implementation;
using CheapSerial.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CheapSerial
{
    /// <summary>
    /// Extension methods for registering serial port services
    /// </summary>
    public static class SerialPortServiceExtensions
    {
        /// <summary>
        /// Registers serial port services with the DI container
        /// </summary>
        public static IServiceCollection AddSerialPortServices(this IServiceCollection services)
        {
            services.AddTransient<ISerialPortFactory, SerialPortFactory>();
            services.AddSingleton<ISerialPortService, SerialPortService>();

            return services;
        }

        /// <summary>
        /// Registers serial port services with configuration from IConfiguration
        /// </summary>
        public static IServiceCollection AddSerialPortServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SerialPortConfiguration>(configuration.GetSection(SerialPortConfiguration.SectionName));
            return services.AddSerialPortServices();
        }

        /// <summary>
        /// Registers serial port services with programmatic configuration
        /// </summary>
        public static IServiceCollection AddSerialPortServices(this IServiceCollection services,
            Action<SerialPortConfiguration> configureOptions)
        {
            services.Configure(configureOptions);
            return services.AddSerialPortServices();
        }
    }
}