using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace Cinteros.XTB.BulkDataUpdater
{
    public partial class BulkDataUpdater
    {
        private bool UpdateState(Entity record, List<BulkActionItem> attributes)
        {
            var attribute = attributes.FirstOrDefault(a => a.Attribute.Metadata is StateAttributeMetadata);
            if (attribute == null)
            {
                return false;
            }
            var statevalue = GetCurrentStateCodeFromStatusCode(attributes);
            var statusvalue = attribute.Value as OptionSetValue;
            var currentstate = record.Contains("statecode") ? record["statecode"] : new OptionSetValue(-1);
            var currentstatus = record.Contains("statuscode") ? record["statuscode"] : new OptionSetValue(-1);
            if (attribute.Action == BulkActionAction.Touch)
            {
                statevalue = (OptionSetValue)currentstate;
                statusvalue = (OptionSetValue)currentstatus;
            }
            if (!attribute.DontTouch || !ValuesEqual(currentstate, statevalue) || !ValuesEqual(currentstatus, statusvalue))
            {
                var req = new SetStateRequest()
                {
                    EntityMoniker = record.ToEntityReference(),
                    State = statevalue,
                    Status = statusvalue
                };
                var resp = Service.Execute(req);
                if (record.Contains("statecode"))
                {
                    record["statecode"] = statevalue;
                }
                else
                {
                    record.Attributes.Add("statecode", statevalue);
                }
                if (record.Contains("statuscode"))
                {
                    record["statuscode"] = currentstatus;
                }
                else
                {
                    record.Attributes.Add("statuscode", currentstatus);
                }
                return true;
            }
            return false;
        }

    }
}
