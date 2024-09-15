﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XRM.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using XrmToolBox.Extensibility;
using XrmToolBox.ToolLibrary.AppCode;

namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    [XmlInclude(typeof(OptionSetValue))]
    [XmlInclude(typeof(OptionSetValueCollection))]
    [XmlInclude(typeof(EntityReference))]
    [XmlInclude(typeof(Money))]
    public class BDULogRun
    {
        private IOrganizationService service;
        private EntityMetadata entitymeta;

        public DateTime TimeStamp = DateTime.Now;
        public string Action;
        public int RecordCount;
        public TimeSpan Duration;
        public int Errors = 0;
        public List<BDULogRequest> Requests = new List<BDULogRequest>();

        public string EntityName => entitymeta?.LogicalName;

        private BDULogRun()
        { }

        public BDULogRun(IOrganizationService service, string action, EntityMetadata entitymeta, int recordcount)
        {
            this.service = service;
            this.entitymeta = entitymeta;
            Action = action;
            RecordCount = recordcount;
        }

        public BDULogRequest AddRequest(IEnumerable<BDUEntity> records)
        {
            var req = new BDULogRequest(service, records);
            Requests.Add(req);
            req.No = Requests.Count;
            return req;
        }

        public void Start() => TimeStamp = DateTime.Now;

        public void Finished()
        {
            Duration = (DateTime.Now - TimeStamp);
            Requests.Where(r => r.Duration == null || r.Duration.TotalMilliseconds == 0).ToList().ForEach(r => r.Finished());
            Requests.ForEach(r => Errors += r.Success ? 0 : 1);
        }

        public string FileName => $"BDU_{Action}_{EntityName}_{TimeStamp:yyyyMMdd_HHmmss}";

        public void SaveLog(PluginControlBase tool)
        {
            string path = Path.Combine(Paths.LogsPath, FileName + ".log");
            try
            {
                if (!Directory.Exists(Paths.LogsPath))
                {
                    Directory.CreateDirectory(Paths.LogsPath);
                }
                XmlSerializerHelper.SerializeToFile(this, path);
            }
            catch (Exception ex)
            {
                tool.LogError($"Saving settings to {path}\n{ex}");
            }
        }

        public void SaveText(PluginControlBase tool, string filepath, char separator)
        {
            try
            {
                var content = ToFlatten(separator);
                File.WriteAllText(filepath, content);
            }
            catch (Exception ex)
            {
                tool.ShowErrorDialog(ex, $"Error saving log", filepath);
            }
        }

        private string ToFlatten(char separator)
        {
            var rows = new List<List<string>>()
            {
                new List<string> { "Req.No", "Request", "Request Id", "Duration", "Unit", "Success", "Error", "Record", "Record Id", "Action", "Attribute", "New value", "Old value" }
            };
            rows.AddRange(Requests.SelectMany(r => r.ToFlatten()));

            RemoveUninterrestColumns(rows);

            var headerrow = 0;
            var headercol = rows.Max(r => r.Count) + 1;
            OnTheRight(rows, headerrow++, headercol, "Date", TimeStamp.ToShortDateString());
            OnTheRight(rows, headerrow++, headercol, "Time", TimeStamp.ToString("HH:mm:ss"));
            OnTheRight(rows, headerrow++, headercol, "Action", Action);
            OnTheRight(rows, headerrow++, headercol, "Entity", EntityName);
            if (entitymeta?.ToDisplayName() is string table && !string.IsNullOrEmpty(table))
            {
                OnTheRight(rows, headerrow++, headercol, "Table", entitymeta?.ToDisplayName());
            }
            OnTheRight(rows, headerrow++, headercol, "Records", RecordCount.ToString());
            if (Duration.TotalMilliseconds > 0)
            {
                var (duration, unit) = Duration.ToSmartStringSplit();
                OnTheRight(rows, headerrow++, headercol, "Duration", duration, unit);
            }
            OnTheRight(rows, headerrow++, headercol, "Errors", Errors.ToString());
            return string.Join(Environment.NewLine, rows.Select(s => Join(separator, s)));
        }

        private void RemoveUninterrestColumns(List<List<string>> rows)
        {
            var col = 0;
            while (col < rows.Max(r => r.Count))
            {
                if (rows.Skip(1).All(r => r.Count <= col || string.IsNullOrWhiteSpace(r[col])) ||
                    (rows[0][col] == "Success" &&
                     (rows.Skip(1).All(r => r.Count <= col || r[col] == "True" || string.IsNullOrWhiteSpace(r[col])))) ||
                    (rows[0][col] == "Old value" &&
                     (rows.Skip(1).All(r => r.Count <= col || r[col] == "<unknown>" || string.IsNullOrWhiteSpace(r[col])))))
                {
                    rows.Where(r => r.Count > col).ToList().ForEach(r => r.RemoveAt(col));
                }
                else
                {
                    col++;
                }
            }
        }

        private static string Join(char separator, List<string> strings)
        {
            if (strings == null || strings.Count == 0)
            {
                return "\"\"";
            }
            return string.Join(separator.ToString(), strings.Select(s => string.IsNullOrEmpty(s) ? string.Empty : s.Contains(separator) || s.Contains('\n') ? "\"" + s + "\"" : s));
        }

        private void OnTheRight(List<List<string>> rows, int rowno, int fromcol, params string[] cells)
        {
            if (rows.Count <= rowno)
            {
                rows.Add(new List<string>());
            }
            var row = rows[rowno];
            if (row.Count < fromcol)
            {
                row.AddRange(new string[fromcol - row.Count]);
            }
            row.AddRange(cells);
        }
    }

    public class BDULogRequest
    {
        public DateTime TimeStamp;
        public int No = 0;
        public string Request = "<none>";
        public Guid Id;
        public TimeSpan Duration;
        public bool Success;
        public string ErrorMessage;
        public List<BDULogRecord> Records = new List<BDULogRecord>();

        private BDULogRequest()
        { }

        internal BDULogRequest(IOrganizationService service, IEnumerable<BDUEntity> records) => Records.AddRange(records.Select(record => new BDULogRecord(service, record)));

        internal void Start() => TimeStamp = DateTime.Now;

        internal void Finished() => Duration = (DateTime.Now - TimeStamp);

        internal List<List<string>> ToFlatten()
        {
            var (duration, unit) = Duration.ToSmartStringSplit();
            var row = new List<string> { No.ToString(), Request, Id.ToString(), duration, unit, Success.ToString(), ErrorMessage };
            var rows = new List<List<string>>();
            rows.AddRange(Records.SelectMany(r => r.ToFlatten(false)));
            if (rows.Count == 0)
            {
                rows.Add(row);
            }
            else
            {
                rows[0].InsertRange(0, row);
            }
            rows.Skip(1).ToList().ForEach(c => c.InsertRange(0, Enumerable.Range(0, row.Count).Select(s => string.Empty).ToList()));
            return rows;
        }
    }

    public class BDULogRecord
    {
        private BDUEntity record;
        public List<BDULogAttribute> Attributes = new List<BDULogAttribute>();

        public Guid Id;
        public string Name;

        private BDULogRecord()
        { }

        public BDULogRecord(IOrganizationService service, BDUEntity record)
        {
            this.record = record;
            Id = record.Id;
            Name = record.Name;
            Attributes.AddRange(record.Attributes.Select(a => new BDULogAttribute(service, record, a.Key)));
        }

        internal List<List<string>> ToFlatten(bool fillcells)
        {
            var row = new List<string> { Name, Id.ToString() };
            var rows = new List<List<string>>();
            rows.AddRange(Attributes.Select(a => a.ToFlatten(true)));
            if (rows.Count == 0)
            {
                rows.Add(row);
            }
            else
            {
                rows[0].InsertRange(0, row);
            }
            rows.Skip(1).ToList().ForEach(c => c.InsertRange(0, Enumerable.Range(0, row.Count).Select(s => string.Empty).ToList()));
            return rows;
        }
    }

    public class BDULogAttribute
    {
        public string LogicalName;
        public string DisplayName;
        public string Action;
        public object NewValue;
        public string NewValueString;
        public object OldValue;
        public string OldValueString;
        public bool OldValueKnown;

        private BDULogAttribute()
        { }

        public BDULogAttribute(IOrganizationService service, BDUEntity entity, string attribute)
        {
            LogicalName = attribute;
            DisplayName = service.GetAttribute(entity.LogicalName, attribute)?.ToDisplayName();
            Action = entity.Action.ContainsKey(attribute) ? entity.Action[attribute] : string.Empty;
            entity.TryGetAttributeValue(attribute, out NewValue);
            NewValueString = entity.AttributeToString(attribute, service);
            if (entity.OldAttribues.Contains(LogicalName) && entity.OldAttribues[LogicalName] != null)
            {
                OldValue = entity.OldAttribues[LogicalName];
                OldValueString = entity.OldFormatted[LogicalName];
                OldValueKnown = true;
            }
        }

        internal List<string> ToFlatten(bool fillcells) => new List<string> { Action, DisplayName, NewValueString, OldValueString };
    }
}