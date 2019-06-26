using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace Cinteros.XTB.BulkDataUpdater
{
    public partial class BulkDataUpdater
    {
        private void DeleteRecords()
        {
            if (working)
            {
                return;
            }
            if (MessageBox.Show("All selected records will unconditionally be deleted.\n" +
                "UI defined rules will NOT be enforced.\n" +
                "Plugins and workflows WILL trigger.\n" +
                "User privileges WILL be respected.\n\n" +
                "Confirm delete!",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            tsbCancel.Enabled = true;
            splitContainer1.Enabled = false;
            working = true;
            var includedrecords = GetIncludedRecords();
            var ignoreerrors = chkDelIgnoreErrors.Checked;
            if (!int.TryParse(cmbDelBatchSize.Text, out int batchsize))
            {
                batchsize = 1;
            }
            WorkAsync(new WorkAsyncInfo()
            {
                Message = "Deleting records",
                IsCancelable = true,
                Work = (bgworker, workargs) =>
                {
                    var sw = Stopwatch.StartNew();
                    var total = includedrecords.Entities.Count;
                    var current = 0;
                    var deleted = 0;
                    var failed = 0;
                    var batch = new ExecuteMultipleRequest
                    {
                        Settings = new ExecuteMultipleSettings { ContinueOnError = ignoreerrors },
                        Requests = new OrganizationRequestCollection()
                    };
                    foreach (var record in includedrecords.Entities)
                    {
                        if (bgworker.CancellationPending)
                        {
                            workargs.Cancel = true;
                            break;
                        }
                        current++;
                        var pct = 100 * current / total;
                        try
                        {
                            if (batchsize == 1)
                            {
                                bgworker.ReportProgress(pct, $"Deleting record {current} of {total}");
                                Service.Delete(record.LogicalName, record.Id);
                                deleted++;
                            }
                            else
                            {
                                batch.Requests.Add(new DeleteRequest { Target = record.ToEntityReference() });
                                if (batch.Requests.Count == batchsize || current == total)
                                {
                                    bgworker.ReportProgress(pct, $"Deleting records {current - batch.Requests.Count + 1}-{current} of {total}");
                                    var response = (ExecuteMultipleResponse)Service.Execute(batch);
                                    if (response.IsFaulted && !ignoreerrors)
                                    {
                                        var firsterror = response.Responses.First(r => r.Fault != null);
                                        if (firsterror != null)
                                        {
                                            throw new Exception(firsterror.Fault.Message);
                                        }
                                        throw new Exception("Unknown exception during delete");
                                    }
                                    deleted += response.Responses.Count;
                                    batch.Requests.Clear();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            if (!ignoreerrors)
                            {
                                throw ex;
                            }
                        }
                    }
                    sw.Stop();
                    workargs.Result = new Tuple<int, int, long>(deleted, failed, sw.ElapsedMilliseconds);
                },
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    tsbCancel.Enabled = false;
                    if (completedargs.Error != null)
                    {
                        MessageBox.Show(completedargs.Error.Message, "Delete", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (completedargs.Cancelled)
                    {
                        if (MessageBox.Show("Operation cancelled!\nRun query to get records again, to verify remaining records.", "Cancel", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                        {
                            RetrieveRecords(fetchXml, RetrieveRecordsReady);
                        }
                    }
                    else if (completedargs.Result is Tuple<int, int, long> result)
                    {
                        lblDelStatus.Text = $"{result.Item1} records deleted, {result.Item2} records failed.";
                        LogUse("Deleted", result.Item1, result.Item3);
                        if (result.Item2 > 0)
                        {
                            LogUse("Failed", result.Item2);
                        }
                        if (MessageBox.Show("Delete completed!\nRun query to get records again?", "Bulk Data Updater", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            RetrieveRecords(fetchXml, RetrieveRecordsReady);
                        }
                    }
                    splitContainer1.Enabled = true;
                },
                ProgressChanged = (changeargs) =>
                {
                    SetWorkingMessage(changeargs.UserState.ToString());
                }
            });
        }
    }
}
