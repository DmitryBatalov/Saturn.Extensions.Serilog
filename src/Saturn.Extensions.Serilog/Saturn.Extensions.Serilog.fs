namespace Saturn.Extensions

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

/// <summary> Initial module </summary>
[<AutoOpen>]
module Serilog =
    open Serilog
    open Saturn
    open Giraffe.SerilogExtensions

    type Application.ApplicationBuilder with
        [<CustomOperation("use_serilog_config")>]
        member this.UseSerilogConfig(state, create: unit -> ILogger) =
            let service (s: IServiceCollection) =
                let logger = create ()
                Log.Logger <- logger
                s.AddSingleton<ILogger>(logger)

            let hostConfig (x: IHostBuilder) = x.UseSerilog()

            { state with
                ServicesConfig = service :: state.ServicesConfig
                HostConfigs = hostConfig :: state.HostConfigs }


        /// Replace default logging with Serilog. Sets the config
        [<CustomOperation("use_serilog")>]
        member this.UseSerilog(state) =
            let config () : ILogger =
                LoggerConfiguration()
                    .Destructure.FSharpTypes()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithThreadName()
                    .Enrich.WithThreadId()
                    .WriteTo.Console(Events.LogEventLevel.Information)
                    .WriteTo
                    .File(
                        "./logs/info.log",
                        rollingInterval = RollingInterval.Day,
                        restrictedToMinimumLevel = Events.LogEventLevel.Information
                    )
                    .WriteTo
                    .File(
                        "./logs/errors.log",
                        rollingInterval = RollingInterval.Day,
                        restrictedToMinimumLevel = Events.LogEventLevel.Error
                    )
                    .CreateLogger()
                :> _

            this.UseSerilogConfig(state, config)
