using System;
using Microsoft.Extensions.DependencyInjection;

namespace Skadi;

//TODO 下载管理服务
//TODO QA使用下载服务下载图片
internal static class SkadiApp
{
    public static DateTime StartTime;

    public static readonly IServiceCollection Services = new ServiceCollection();

    public static IServiceScope CreateScope()
    {
        return Services.BuildServiceProvider().CreateScope();
    }

    public static T GetService<T>()
    {
        return Services.BuildServiceProvider().GetService<T>();
    }
}