using System;
using PuppeteerSharp;
using Skadi.Services;
using Sora.Command;

namespace Skadi;

internal static class StaticStuff
{
    public static QaConfigService QaConfig;

    public static CommandManager CommandManager;

    public static DateTime StartTime;

    //TODO IOC
    public static IBrowser Chrome;
}