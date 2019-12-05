using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace Cinteros.XTB.BulkDataUpdater
{
    public partial class BulkDataUpdater
    {
        private void AssignRecords()
        {
            if (working)
            {
                return;
            }
            var owner = cbAssignUser.SelectedEntity != null ? cbAssignUser.SelectedEntity : cbAssignTeam.SelectedEntity;
            var ownername = cbAssignUser.SelectedEntity != null ? "User " + cbAssignUser.Text : "Team " + cbAssignTeam.Text;
            if (owner == null)
            {
                return;
            }
            if (MessageBox.Show($"All selected records will unconditionally be reassigned to {ownername}.\n" +
                "UI defined rules will NOT be enforced.\n" +
                "Plugins and workflows WILL trigger.\n" +
                "User privileges WILL be respected.\n\n" +
                "Please confirm assignment.",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            tsbCancel.Enabled = true;
            splitContainer1.Enabled = false;
            working = true;
            var includedrecords = GetIncludedRecords();
            var ignoreerrors = chkIgnoreErrors.Checked;
            if (!int.TryParse(cmbBatchSize.Text, out int batchsize))
            {
                batchsize = 1;
            }
            WorkAsync(new WorkAsyncInfo()
            {
                Message = "Assigning records",
                IsCancelable = true,
                Work = (bgworker, workargs) =>
                {
                    var sw = Stopwatch.StartNew();
                    var total = includedrecords.Entities.Count;
                    var current = 0;
                    var assigned = 0;
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
                            var clone = new Entity(record.LogicalName, record.Id);
                            clone.Attributes.Add("ownerid", owner.ToEntityReference());
                            if (batchsize == 1)
                            {
                                bgworker.ReportProgress(pct, $"Assigning record {current} of {total}");
                                Service.Update(clone);
                                assigned++;
                            }
                            else
                            {
                                batch.Requests.Add(new UpdateRequest { Target = clone });
                                if (batch.Requests.Count == batchsize || current == total)
                                {
                                    bgworker.ReportProgress(pct, $"Assigning records {current - batch.Requests.Count + 1}-{current} of {total}");
                                    var response = (ExecuteMultipleResponse)Service.Execute(batch);
                                    if (response.IsFaulted && !ignoreerrors)
                                    {
                                        var firsterror = response.Responses.First(r => r.Fault != null);
                                        if (firsterror != null)
                                        {
                                            throw new Exception(firsterror.Fault.Message);
                                        }
                                        throw new Exception("Unknown exception during assignment");
                                    }
                                    assigned += response.Responses.Count;
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
                    workargs.Result = new Tuple<int, int, long>(assigned, failed, sw.ElapsedMilliseconds);
                },
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    tsbCancel.Enabled = false;
                    if (completedargs.Error != null)
                    {
                        MessageBox.Show(completedargs.Error.Message, "Assign", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (completedargs.Cancelled)
                    {
                        if (MessageBox.Show("Operation cancelled!\nRun query to get records again, to verify record information.", "Cancel", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                        {
                            RetrieveRecords(fetchXml, RetrieveRecordsReady);
                        }
                    }
                    else if (completedargs.Result is Tuple<int, int, long> result)
                    {
                        lblUpdateStatus.Text = $"{result.Item1} records reassigned, {result.Item2} records failed.";
                        LogUse("Assigned", result.Item1, result.Item3);
                        if (result.Item2 > 0)
                        {
                            LogUse("Failed", result.Item2);
                        }
                        if (MessageBox.Show("Assign completed!\nRun query to get records again?", "Bulk Data Updater", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
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

        private void LoadOwners()
        {
            if (cbAssignUser.DataSource == null)
            {
                cbAssignUser.OrganizationService = Service;
                cbAssignUser.DisplayFormat = $"{{{{{User.PrimaryName}}}}} ({{{{{User.PrimaryEmail}}}}})";
                var qxUser = new QueryExpression(User.EntityName);
                qxUser.ColumnSet.AddColumns(User.PrimaryKey, User.PrimaryName, User.PrimaryEmail, User.BusinessUnit);
                qxUser.AddOrder(User.PrimaryName, OrderType.Ascending);
                qxUser.Criteria.AddCondition(User.Status, ConditionOperator.Equal, false);
                qxUser.Criteria.AddCondition(User.AccessMode, ConditionOperator.NotEqual, (int)User.AccessMode_OptionSet.SupportUser);
                qxUser.Criteria.AddCondition(User.AccessMode, ConditionOperator.NotEqual, (int)User.AccessMode_OptionSet.DelegatedAdmin);
                WorkAsync(new WorkAsyncInfo
                {
                    Message = "Loading Users",
                    AsyncArgument = qxUser,
                    Work = (worker, args) =>
                    {
                        args.Result = Service.RetrieveMultiple(args.Argument as QueryExpression);
                    },
                    PostWorkCallBack = (args) =>
                    {
                        if (args.Error != null)
                        {
                            MessageBox.Show(args.Error.Message, "Getting users", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else if (args.Result is EntityCollection users)
                        {
                            cbAssignUser.DataSource = users;
                            cbAssignUser.SelectedIndex = -1;
                        }
                    }
                });
            }

            if (cbAssignTeam.DataSource == null)
            {
                cbAssignTeam.OrganizationService = Service;
                cbAssignTeam.DisplayFormat = $"{{{{{Team.PrimaryName}}}}} ({{{{{Team.BusinessUnit}}}}})";
                var qxTeam = new QueryExpression(Team.EntityName);
                qxTeam.ColumnSet.AddColumns(Team.PrimaryKey, Team.PrimaryName, Team.BusinessUnit);
                qxTeam.AddOrder(Team.PrimaryName, OrderType.Ascending);
                //qxTeam.Criteria.AddCondition(Team.TeamType, ConditionOperator.Equal, (int)Team.TeamType_OptionSet.Owner);
                WorkAsync(new WorkAsyncInfo
                {
                    Message = "Loading Teams",
                    AsyncArgument = qxTeam,
                    Work = (worker, args) =>
                    {
                        args.Result = Service.RetrieveMultiple(args.Argument as QueryExpression);
                    },
                    PostWorkCallBack = (args) =>
                    {
                        if (args.Error != null)
                        {
                            MessageBox.Show(args.Error.Message, "Getting Teams", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else if (args.Result is EntityCollection teams)
                        {
                            cbAssignTeam.DataSource = teams;
                            cbAssignTeam.SelectedIndex = -1;
                        }
                    }
                });
            }
        }
    }
}
