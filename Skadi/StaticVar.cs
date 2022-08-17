using System;
using System.Threading;
using PuppeteerSharp;
using Skadi.IO;
using Sora.Command;

namespace Skadi;

internal static class StaticVar
{
    public static DateTime StartTime;

    public static CommandManager SoraCommandManager;

    public static QAConfigFile QaConfigFile;

    public static readonly AutoResetEvent ServiceReady = new(false);

    public static Browser Chrome;
}