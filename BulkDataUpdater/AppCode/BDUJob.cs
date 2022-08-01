using McTools.Xrm.Connection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    public class BDUJob
    {
        private string fetchXML;

        public JobInfo Info { get; set; }
        public string Entity { get; set; }

        public string FetchXML
        {
            get => fetchXML;
            set
            {
                fetchXML = null;
                Entity = null;
                try
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(value);
                    fetchXML = value;
                    Entity = doc.SelectSingleNode("fetch/entity").Attributes["name"].Value;
                }
                catch (Exception)
                {
                    fetchXML = null;
                    Entity = null;
                }
            }
        }

        public bool IncludeAll { get; set; }
        public JobUpdate Update { get; set; } = new JobUpdate();
        public JobAssign Assign { get; set; } = new JobAssign();
        public JobSetState SetState { get; set; } = new JobSetState();
        public JobDelete Delete { get; set; } = new JobDelete();
    }

    public class JobInfo
    {
        public string Name { get; set; }
        public string OriginalPath { get; set; }
        public DateTime CreatedOn { get; set; }
        public string EnvironmentURL { get; set; }

        private JobInfo()
        { }

        public JobInfo(string path, ConnectionDetail connectionDetail)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            if (Name.EndsWith(".bdu"))
            {
                Name = Name.Substring(0, Name.Length - 4);
            }
            OriginalPath = path;
            CreatedOn = DateTime.Now;
            EnvironmentURL = connectionDetail.OriginalUrl;
        }
    }

    public class JobUpdate
    {
        public List<BulkActionItem> Attributes { get; set; } = new List<BulkActionItem>();
        public JobExecuteOptions ExecuteOptions { get; set; } = new JobExecuteOptions();
    }

    public class JobAssign
    {
        public string Entity { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public JobExecuteOptions ExecuteOptions { get; set; } = new JobExecuteOptions();
    }

    public class JobSetState
    {
        public int State { get; set; }
        public int Status { get; set; }
        public string StateName { get; set; }
        public string StatusName { get; set; }
        public JobExecuteOptions ExecuteOptions { get; set; } = new JobExecuteOptions();
    }

    public class JobDelete
    {
        public JobExecuteOptions ExecuteOptions { get; set; } = new JobExecuteOptions();
    }

    public class JobExecuteOptions
    {
        public int DelayCallTime { get; set; } = 0;
        public int BatchSize { get; set; } = 1;
        public bool IgnoreErrors { get; set; } = false;
        public bool BypassCustom { get; set; } = false;
    }
}