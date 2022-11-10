using System;
using Microsoft.Extensions.DependencyInjection;

namespace Skadi;

internal static class StaticStuff
{
    public static DateTime StartTime;

    public static readonly IServiceCollection Services = new ServiceCollection();

    public static IServiceProvider ServiceProvider => Services.BuildServiceProvider();
}