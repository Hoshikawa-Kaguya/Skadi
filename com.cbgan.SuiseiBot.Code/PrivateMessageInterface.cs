using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code
{
    public class PrivateMessageInterface : IPrivateMessage
    {
        public void PrivateMessage(object sender, CQPrivateMessageEventArgs e)
        {
            QQ id = e.FromQQ;
            if (e.Message.Text.Equals("debug"))
            {
                string Curr_Dir = System.IO.Directory.GetCurrentDirectory() + "\\data";
                id.SendPrivateMessage(Curr_Dir);
                //string FilePath = Curr_Dir + '\\' + "NewFile.txt";
                //FileStream fs = new FileStream(FilePath, FileMode.CreateNew);
                //StreamWriter sw = new StreamWriter(fs);
                //sw.Write("哇哦");  //这里是写入的内容             
                //sw.Flush();
            }
            e.Handler = true;
        }
    }
}
