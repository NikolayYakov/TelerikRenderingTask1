using Microsoft.SqlServer.Server;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Telerik.Reporting;
using Telerik.Reporting.Processing;
 

namespace TelerikRenderingTask1
{
    internal class Program
    {
        private static readonly string _path = @"C:\Program Files (x86)\Progress\Telerik Report Server\Telerik.ReportServer.Web\SampleReports\";
        private static ConcurrentDictionary<string, long> _reportsRenderTimeDic = new ConcurrentDictionary<string, long>();

        static async Task Main(string[] args)
        {
            //initializing
            var reportProcessor = new ReportProcessor();
            var deviceInfo = new Hashtable();
            var reportSource = new UriReportSource();

            string[] reportsToRenderName = new string[3] { "Dashboard.trdp", "Product Line Sales.trdp", "Employee Sales Summary.trdp" };
            string format = "PDF";

            try
            {
                //i do this because reportProcessor sometimes throw NullReferenceException 
                //i found this thread https://feedback.telerik.com/reporting/1414174-occasional-nullreferenceexception-on-rendering
                //the answer was that when you do multi-thread rendering this error sometimes appear
                //and The workaround is initially to run a single report in the main thread - this will initialize the static dictionary holding the resources. 
                //it seems to fix the problem 
                //please ignore this part of the code
                reportSource.Uri = _path + "Dashboard.trdp";
                reportProcessor.RenderReport(format, reportSource, deviceInfo);

                //start 10 task for every report
                List<Task> Tasks = new List<Task>();
                foreach (var reportName in reportsToRenderName)
                {
                    reportSource.Uri = _path + reportName;
                    for (int i = 0; i < 10; i++)
                    {
                        Tasks.Add(Task.Run(() => RenderReport(reportProcessor, reportSource, deviceInfo, format, reportName)));
                    }
                }
    
                await Task.WhenAll(Tasks);

                PrintResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void RenderReport(ReportProcessor reportProcessor, UriReportSource reportSource, Hashtable deviceInfo, string format, string reportName)
        {
            var stopWatch = new Stopwatch();

            stopWatch.Start();
            reportProcessor.RenderReport(format, reportSource, deviceInfo);
            stopWatch.Stop();

            //if dictionary don't contains the key add, if stopwatch value is smaller than the dic value modify.
            if (!_reportsRenderTimeDic.ContainsKey(reportName) || _reportsRenderTimeDic[reportName] > stopWatch.ElapsedMilliseconds)
            {
                _reportsRenderTimeDic[reportName] = stopWatch.ElapsedMilliseconds;
            }
        }

        private static void PrintResult()
        {
            foreach(var reportRenderTime in _reportsRenderTimeDic)
            {
                Console.WriteLine($"{reportRenderTime.Key} : {reportRenderTime.Value}ms");
            }
        }
    }
}