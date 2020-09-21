using System;
using System.Runtime.InteropServices;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Expand;
using Native.Sdk.Cqp.Model;
using Newtonsoft.Json;
using SuiseiBot.Code;
using SuiseiBot.Code.CQInterface;
using SuiseiBot.Code.Tool.LogUtils;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local

namespace SuiseiBot.Export
{
	/// <summary>	
	/// 表示酷Q事件导出的类	
	/// </summary>	
	public class EventExport	
	{
        #region 属性
        //插件的所有信息
        private static readonly PluginInfo pInfo = new PluginInfo();
        //CqApi
        private static CQApi cqApi { get; set; }
        //CqLog
        private static CQLog cqLog { get; set; }
        #endregion

        #region --插件信息--
        /// <summary>	
		/// 返回酷Q用于识别本应用的 AppID 和 ApiVer	
		/// </summary>	
		/// <returns>酷Q用于识别本应用的 AppID 和 ApiVer</returns>	
		[DllExport (ExportName = "AppInfo", CallingConvention = CallingConvention.StdCall)]	
		private static string AppInfo ()	
		{	
			return $"{pInfo.apiver},{pInfo.name}";	
		}	
        
        /// <summary>
        /// native插件信息
        /// </summary>
        [DllExport (ExportName = "pluginInfo", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr pluginInfo()
        {
            return Marshal.StringToHGlobalAnsi(JsonConvert.SerializeObject(new PluginInfo()));
        }
		#endregion

        #region 插件初始化
        /// <summary>	
        /// 接收应用 Authcode, 用于注册接口	
        /// </summary>	
        /// <param name="authCode">酷Q应用验证码</param>	
        /// <returns>返回注册结果给酷Q</returns>	
        [DllExport (ExportName = "Initialize", CallingConvention = CallingConvention.StdCall)]	
        private static int Initialize (int authCode)	
        {
            AppInfo appInfo = new AppInfo (pInfo.name, pInfo.ret, pInfo.apiver, pInfo.name, pInfo.version, pInfo.version_id, pInfo.author, pInfo.description, authCode);
            cqApi = new CQApi(appInfo);
            cqLog = new CQLog(authCode);
            // 注册插件全局异常捕获回调, 用于捕获未处理的异常
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            // 事件回调
            Event_eventEnableHandler     += AppEnableInterface.AppEnable;
            Event_eventGroupMsgHandler   += GroupMessageInterface.GroupMessage;
            Event_eventPrivateMsgHandler += PrivateMessageInterface.PrivateMessage;
            return 0;	
        }
        #endregion
		
		#region --私有方法--	
		/// <summary>	
		/// 全局异常捕获, 用于捕获开发者未处理的异常, 此异常将回弹至酷Q进行处理	
		/// </summary>	
		/// <param name="sender">事件来源对象</param>	
		/// <param name="e">附加的事件参数</param>	
		private static void CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)	
		{
            if (e.ExceptionObject is Exception ex)
            {
                ConsoleLog.UnhandledExceptionLog(ex);
            }	
		}
        #endregion

        #region --导出方法及事件回调--	
		/// <summary>	
		/// 事件回调, 以下是对应 Json 文件的信息	
		/// <para>Id: 1</para>	
		/// <para>Type: 21</para>	
		/// <para>Name: 私聊消息处理</para>	
		/// <para>Function: _eventPrivateMsg</para>	
		/// <para>Priority: 30000</para>	
		/// <para>IsRegex: False</para>	
		/// </summary>
		public static event EventHandler<CQPrivateMessageEventArgs> Event_eventPrivateMsgHandler;	
        [DllExport (ExportName = "_eventPrivateMsg", CallingConvention = CallingConvention.StdCall)]	
		public static int Event_eventPrivateMsg (int subType, int msgId, long fromQQ, IntPtr msg, int font)	
		{	
			if (Event_eventPrivateMsgHandler != null)	
			{	
				CQPrivateMessageEventArgs args = new CQPrivateMessageEventArgs (cqApi, cqLog, 1, 21, "PrivateMessageInterface", "_eventPrivateMsg", 30000, subType, msgId, fromQQ, msg.ToString(CQApi.DefaultEncoding), false);	
				Event_eventPrivateMsgHandler (typeof (EventExport), args);	
				return (int)(args.Handler ? CQMessageHandler.Intercept : CQMessageHandler.Ignore);	
			}	
			return 0;	
		}	
		
		/// <summary>	
		/// 事件回调, 以下是对应 Json 文件的信息	
		/// <para>Id: 2</para>	
		/// <para>Type: 2</para>	
		/// <para>Name: 群消息处理</para>	
		/// <para>Function: _eventGroupMsg</para>	
		/// <para>Priority: 30000</para>	
		/// <para>IsRegex: False</para>	
		/// </summary>	
		public static event EventHandler<CQGroupMessageEventArgs> Event_eventGroupMsgHandler;	
		[DllExport (ExportName = "_eventGroupMsg", CallingConvention = CallingConvention.StdCall)]	
		public static int Event_eventGroupMsg (int subType, int msgId, long fromGroup, long fromQQ, string fromAnonymous, IntPtr msg, int font)	
		{	
			if (Event_eventGroupMsgHandler != null)	
			{	
				CQGroupMessageEventArgs args = new CQGroupMessageEventArgs (cqApi, cqLog, 2, 2, "GroupMessageInterface", "_eventGroupMsg", 30000, subType, msgId, fromGroup, fromQQ, fromAnonymous, msg.ToString(CQApi.DefaultEncoding), false);	
				Event_eventGroupMsgHandler (typeof (EventExport), args);	
				return (int)(args.Handler ? CQMessageHandler.Intercept : CQMessageHandler.Ignore);	
			}	
			return 0;	
		}

        /// <summary>	
		/// 事件回调, 以下是对应 Json 文件的信息	
		/// <para>Id: 1003</para>	
		/// <para>Type: 1003</para>	
		/// <para>Name: 应用已被启用</para>	
		/// <para>Function: _eventEnable</para>	
		/// <para>Priority: 30000</para>	
		/// <para>IsRegex: False</para>	
		/// </summary>	
		public static event EventHandler<CQAppEnableEventArgs> Event_eventEnableHandler;	
		[DllExport (ExportName = "_eventEnable", CallingConvention = CallingConvention.StdCall)]	
		public static int Event_eventEnable ()	
		{	
			if (Event_eventEnableHandler != null)	
			{	
				CQAppEnableEventArgs args = new CQAppEnableEventArgs (cqApi, cqLog, 1003, 1003, "AppEnableInterface", "_eventEnable", 30000);	
				Event_eventEnableHandler (typeof (EventExport), args);	
			}	
			return 0;	
		}
        #endregion
	}	
}
