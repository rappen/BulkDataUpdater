using System;
using System.Collections.Generic;
using System.Xml;

namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    public class BDUJob
    {
        private string fetchXML;

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

    public class JobUpdate
    {
        public List<BulkActionItem> Attributes { get; set; } = new List<BulkActionItem>();
        public JobExecuteOptions ExecuteOptions { get; set; }
    }

    public class JobAssign
    {
        public string Entity { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public JobExecuteOptions ExecuteOptions { get; set; }
    }

    public class JobSetState
    {
        public int State { get; set; }
        public int Status { get; set; }
        public string StateName { get; set; }
        public string StatusName { get; set; }
        public JobExecuteOptions ExecuteOptions { get; set; }
    }

    public class JobDelete
    {
        public JobExecuteOptions ExecuteOptions { get; set; }
    }

    public class JobExecuteOptions
    {
        public int DelayCallTime { get; set; } = 0;
        public int BatchSize { get; set; } = 1;
        public bool IgnoreErrors { get; set; } = false;
        public bool BypassCustom { get; set; } = false;
    }
}