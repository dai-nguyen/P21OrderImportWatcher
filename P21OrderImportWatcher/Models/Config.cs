/** 
 * This file is part of the P21OrderImportWatcher project.
 * Copyright (c) 2014 Dai Nguyen
 * Author: Dai Nguyen
**/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P21OrderImportWatcher.Models
{
    public class Config
    {
        public int WaitInSeconds { get; set; }
        public string ActiveFolder { get; set; }
        public string ErrorFolder { get; set; }
        public string SummaryFolder { get; set; }

        public string[] EmailTos { get; set; }
        public string DefaultMail { get; set; }

        public DbMailConfig DbMail { get; set; }
        public SmtpMailConfig SmtpMail { get; set; }        
    }

    public class DbMailConfig
    {
        public string Server { get; set; }
        public string Db { get; set; }
        public string StoredProcedure { get; set; }
        public string Profile { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
    }

    public class SmtpMailConfig
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
        public bool EnableSSL { get; set; }
        public string EmailFrom { get; set; }
    }    
}
