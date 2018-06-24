using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using CsQuery;
using System.Collections.Specialized;
using CsQuery.Output;
using System.IO;

namespace TaskLearn
{
    class Program
    {
        static string url = "http://www.hrssgz.gov.cn/vsgzpiapp01/GZPI/Gateway/PersonIntroducePublicity.aspx";

        static void Main(string[] args)
        {
            string viewstate = GetViewstate();
            //Task task = Task.Factory.StartNew(LoadUser, viewstate);
            
            for (int i = 1; i <= 10; i++)
            {
                PageIndex pageIndex = new PageIndex { page = i, viewstate = viewstate };
                Task<string> taskMin =  Task.Factory.StartNew(LoadUser, pageIndex);
                taskMin.Wait();
                if (taskMin.Status == TaskStatus.RanToCompletion)
                    viewstate = taskMin.Result;
            }

            Console.ReadKey();
        }

        private class PageIndex
        {
            public int page { get; set; }
            public string viewstate { get; set; }
        }

        private static string GetViewstate()
        {
            WebClientEx client = new WebClientEx();
            string Page = client.Get(url); //get some cookies and viewstate

            CQ dom = CQ.Create(Page);
            string viewstate = dom["input#__VIEWSTATE"].Val(); //get viewstate

            return viewstate;
        }

        private static string LoadUser(object pageIndex)
        {
            PageIndex pageInfo = (PageIndex)pageIndex;
            WebClientEx client = new WebClientEx();
            NameValueCollection data = new NameValueCollection();
            data.Add("__EVENTTARGET",   pageInfo.page != 1? "NextLBtn":"");
            data.Add("__EVENTARGUMENT", "");
            data.Add("__VIEWSTATE", pageInfo.viewstate.ToString()); //your string
            data.Add("__VIEWSTATEGENERATOR", "3C84D956");
            //data.Add("ToPage", "7");

            // IDomObject
            CsQuery.Config.HtmlEncoder = new TT();
            string Page = client.Post(url, data);
            CQ dom = CQ.Create(Page);
            pageInfo.viewstate = dom["input#__VIEWSTATE"].Val();
            var value = dom["#data_field"]["tr.ListItem,tr.ListAltern"]
                .Elements
                .Select(
                    t =>
                    {
                        CQ tdCq = CQ.Create(t.InnerHTML)["td"];
                        var user = new
                        {
                            name = tdCq[0].InnerText,
                            company = tdCq[1].InnerText,
                            state = tdCq[2].InnerText,
                            gv = tdCq[3].InnerText,
                            begindate = tdCq[4].InnerText,
                            enddate = tdCq[5].InnerText
                        };
                        return user;
                    });
            using (FileStream fs = File.Open("1.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                StringBuilder sb = new StringBuilder();
                foreach (var item in value)
                {
                    sb.AppendLine(item.name + "," + item.company + "," + item.state + "," + item.gv + "," + item.begindate + "," + item.enddate);
                }
                byte[] targetByte = Encoding.UTF8.GetBytes(sb.ToString());
                fs.Position = fs.Length;
                fs.Write(targetByte, 0, targetByte.Length);
                fs.Flush();
                Console.WriteLine(pageInfo.page +" success to write in file");
                return pageInfo.viewstate.ToString();
            }
        }

        public class TT : IHtmlEncoder
        {
            public void Encode(string text, TextWriter output)
            {
                output.Write(text);
            }
        }



    }
}
