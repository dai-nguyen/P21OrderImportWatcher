/** 
 * This file is part of the P21OrderImportWatcher project.
 * Copyright (c) 2014 Dai Nguyen
 * Author: Dai Nguyen
**/

using Newtonsoft.Json;
using P21OrderImportWatcher.DataAccess;
using P21OrderImportWatcher.Models;
using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace P21OrderImportWatcher
{
    public partial class P21OrderImportWatcherService : ServiceBase
    {
        CancellationTokenSource _source;
        FileSystemWatcher _watcher;
        Timer _timer;

        public P21OrderImportWatcherService()
        {
            InitializeComponent();
        }

        private void Initialize()
        {
            _source = new CancellationTokenSource();
            Config config = GetConfig();

            if (config == null)
                return;

            _timer = new Timer(new TimerCallback(CheckImportStatusAsync), null, 1000, config.WaitInSeconds * 1000);

            _watcher = new FileSystemWatcher();
            _watcher.Filter = "*.txt";
            _watcher.Path = config.ActiveFolder;
            _watcher.Deleted += new FileSystemEventHandler(OnChanged);
            _watcher.Created += new FileSystemEventHandler(OnChanged);
            _watcher.EnableRaisingEvents = true;  
        }

        private async void CheckImportStatusAsync(object state)
        {
            var config = GetConfig();

            if (config == null)
                return;

            using (FileImportService service = new FileImportService())
            {
                var list = await service.GetReadyToCheckImports(config.WaitInSeconds);

                foreach (var file in list)
                {
                    file.DateChecked = DateTime.Now;

                    string sumfile = Path.Combine(config.SummaryFolder, Path.GetFileNameWithoutExtension(file.FileName) + ".sum");

                    if (File.Exists(sumfile) && IsImportSucceeded(sumfile))
                    {
                        file.Result = "Imported";
                    }
                    else
                    {
                        file.Result = "Failed";

                        string body = GetAllErrors(file.FileName, config.ErrorFolder, config.SummaryFolder);

                        if (config.DefaultMail == "DbMail")
                        {
                            await service.SendDbMail(config.DbMail, config.EmailTos, "",
                                string.Format("Import Error - {0}", file.FileName), body);
                        }
                        else
                        {
                            await service.SendSmtpEmailAsync(config.SmtpMail, config.EmailTos,
                                string.Format("Import Error - {0}", file.FileName), body);
                        }
                    }
                    await service.UpdateAsync(file, _source.Token);
                }
            }
        }

        private string GetAllErrors(string file, string errorFolder, string sumFolder)
        {
            StringBuilder builder = new StringBuilder();

            string sumfile = Path.Combine(sumFolder, Path.GetFileNameWithoutExtension(file) + ".sum");

            if (File.Exists(sumfile))
            {
                builder.AppendLine(@"<span style=""font-weight:bold"">" + sumfile + "</span><br />");
                builder.AppendLine("<p>" + File.ReadAllText(sumfile).Replace("\n", "<br />") + "</p>");
            }

            Match m = Regex.Match(file, @"\d+");

            foreach (string errorfile in Directory.GetFiles(errorFolder, string.Format("*{0}.err", m.Value.ToString())))
            {
                builder.AppendLine(@"<span style=""font-weight:bold"">" + errorfile + "</span><br />");
                builder.AppendLine("<p>" + File.ReadAllText(errorfile).Replace("\n", "<br />") + "</p>");
            }

            return builder.ToString();
        }

        private bool IsImportSucceeded(string sumfile)
        {
            return File.ReadAllLines(sumfile).Any(t => t.Contains("<order_number 1>"));
        }

        private async void OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created && e.Name.StartsWith("WOH"))
            {
                using (FileImportService service = new FileImportService())
                {
                    var succeeded = await service.CreateAsync(new FileImport
                    {
                        FileName = e.Name
                    }, _source.Token);                    
                }
            }
            else if (e.ChangeType == WatcherChangeTypes.Deleted && e.Name.StartsWith("WOH"))
            {
                using (FileImportService service = new FileImportService())
                {
                    var found = await service.GetByFileNameAsync(e.Name);

                    if (found != null)
                    {
                        found.DateDeleted = DateTime.Now;
                        var succeeded = await service.UpdateAsync(found, _source.Token);                        
                    }
                }
            }
        }

        private Config GetConfig()
        {
            try
            {
                string current = Path.Combine(Directory.GetCurrentDirectory(), "Data");
                string filename = Path.Combine(current, "Config.json");

                if (File.Exists(filename))
                {
                    return JsonConvert.DeserializeObject<Config>(File.ReadAllText(filename));
                }
            }
            catch { }
            return null;
        }

        protected override void OnStart(string[] args)
        {
            Initialize();
        }

        protected override void OnStop()
        {
            _source.Cancel();
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _timer.Dispose();
            _source.Dispose();
        }
    }
}
