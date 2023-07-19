using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Xrm.Sdk;
using Rappen.XTB.Helpers.ControlItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Cinteros.XTB.BulkDataUpdater
{
    public partial class BulkDataUpdater
    {
        private static int importsequencestartsat = int.MinValue + 19710917;
        private static Random random = new Random();

        private void CreateRecords()
        {
            if (working)
            {
                return;
            }
            var entity = "account";
            var number = 3;
            var isn = GetISN();
            var executeoptions = GetExecuteOptions();
            if (MessageBox.Show($"Creating {number} NEW records.\n\n{(executeoptions.BypassCustom ? "NO custom plugins will be run.\n" : "")}UI defined rules will NOT be enforced.\n\nRecords will be found with:\nimportsequencenumber = {isn}\n\nConfirm create!",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            var selectedattributes = lvAttributes.Items.Cast<ListViewItem>().Select(i => i.Tag as BulkActionItem).ToList();
            selectedattributes.Add(GetISNAction(entity, isn));
            var newrecords = GetNewBlankRecords(entity, isn, number);
            CreUpdateRecords(true, newrecords, executeoptions, selectedattributes);
        }

        private IEnumerable<Entity> GetNewBlankRecords(string entityname, int isn, int number)
        {
            var result = new List<Entity>();
            for (var i = 0; i < number; i++)
            {
                result.Add(new Entity(entityname));
            }
            return result;
        }

        private int GetISN()
        {
            return importsequencestartsat + random.Next(1, 1000000);
        }

        private BulkActionItem GetISNAction(string entity, int isn)
        {
            var isnmeta = entities.FirstOrDefault(e => e.LogicalName == entity)?.Attributes?.FirstOrDefault(a => a.LogicalName == "importsequencenumber");
            var isnmetaitem = new AttributeMetadataItem(isnmeta, true, true);
            var isnattr = new BulkActionItem
            {
                Action = BulkActionAction.SetValue,
                Attribute = isnmetaitem,
                AttributeName = isnmeta.LogicalName,
                DontTouch = false,
                EntityName = entity,
                StringValue = isn.ToString(),
                Value = isn
            };
            return isnattr;
        }

        private string GetISNQuery()
        {
            var fetch = @"<fetch aggregate='true' >
  <entity name='account' >
    <attribute name='accountid' alias='Count' aggregate='count' />
    <attribute name='importsequencenumber' alias='ISN' groupby='true' />
    <attribute name='createdon' alias='Created' aggregate='min' />
    <attribute name='createdby' alias='By' groupby='true' />
    <filter type='and' >
      <condition attribute='importsequencenumber' operator='between' >
        <value>-2127772731</value>
        <value>-2126772731</value>
      </condition>
    </filter>
  </entity>
</fetch>";
            return fetch;
        }
    }
}