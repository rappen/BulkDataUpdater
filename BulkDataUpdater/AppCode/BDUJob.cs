﻿using McTools.Xrm.Connection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    public class BDUJob
    {
        private string fetchXML;

        internal BDULogRun Log;

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

        public string LayoutXML { get; set; }
        public JobUpdate Update { get; set; } = new JobUpdate();
        public JobAssign Assign { get; set; } = new JobAssign();
        public JobSetState SetState { get; set; } = new JobSetState();
        public JobDelete Delete { get; set; } = new JobDelete();
        internal IEnumerable<string> SupportMessages { get; set; }
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

    public abstract class JobExecute
    {
        public JobExecuteOptions ExecuteOptions { get; set; } = new JobExecuteOptions();
    }

    public class JobUpdate : JobExecute
    {
        public bool SetImpSeqNo { get; set; } = true;
        public bool DefaultImpSeqNo { get; set; } = true;
        public int ImpSeqNo { get; set; }
        public List<BulkActionItem> Attributes { get; set; } = new List<BulkActionItem>();
    }

    public class JobAssign : JobExecute
    {
        public string Entity { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class JobSetState : JobExecute
    {
        public int? State { get; set; }
        public int? Status { get; set; }
        public string StateName { get; set; }
        public string StatusName { get; set; }
    }

    public class JobDelete : JobExecute
    {
    }

    public class JobExecuteOptions
    {
        public int FormWidth { get; set; }
        public int FormHeight { get; set; }
        public int DelayCallTime { get; set; } = 0;
        public int BatchSize { get; set; } = 1;
        public bool MultipleRequest { get; set; } = true;
        public bool IgnoreErrors { get; set; } = false;
        public bool BypassCustom { get; set; } = false;
        public bool BypassSync { get; set; } = false;
        public bool BypassAsync { get; set; } = false;
        public List<Guid> BypassSteps { get; set; } = new List<Guid>();
        public bool DontAskOptions { get; set; } = false;

        internal int DelayCurrent = 0;
        internal bool DelayNow = false;
    }
}