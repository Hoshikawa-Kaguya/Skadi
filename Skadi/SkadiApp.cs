using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Skadi;

internal static class SkadiApp
{
    public static DateTime StartTime;

    public static readonly IServiceCollection Services = new ServiceCollection();

    public static IServiceScope CreateScope() => Services.BuildServiceProvider().CreateScope();

    public static IEnumerable<T> GetServices<T>() => Services.BuildServiceProvider().GetServices<T>();

    public static T GetService<T>() => Services.BuildServiceProvider().GetService<T>();
}