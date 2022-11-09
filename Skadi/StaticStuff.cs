using System;
using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;
using Skadi.Services;
using Sora.Command;

namespace Skadi;

internal static class StaticStuff
{
    public static CommandManager CommandManager;

    public static DateTime StartTime;

    //TODO IOC
    public static IBrowser Chrome;

    public static readonly IServiceCollection Services = new ServiceCollection();

    public static IServiceProvider ServiceProvider => Services.BuildServiceProvider();
}