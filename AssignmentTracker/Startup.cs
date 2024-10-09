using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AssignmentTracker.Services;
using AssignmentTracker.Interfaces;
using Serilog.Extensions.Logging;

[assembly: FunctionsStartup(typeof(AssignmentTracker.Startup))]
namespace AssignmentTracker
{
    [ExcludeFromCodeCoverage]
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerFactory>(sc =>
            {
                var providerCollection = sc.GetService<LoggerProviderCollection>();
                var factory = new SerilogLoggerFactory(null, true, providerCollection);
                foreach (var provider in sc.GetServices<ILoggerProvider>())
                    factory.AddProvider(provider);
                return factory;
            });
            builder.Services.AddSingleton<IAssignmentService, AssignmentService>();
            //builder.Services.AddSingleton<IClassService, ClassService>();
            //builder.Services.AddSingleton<IPriorityService, PriorityService>();
        }
    }
}