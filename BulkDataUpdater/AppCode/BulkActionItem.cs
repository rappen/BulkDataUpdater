using Microsoft.Xrm.Sdk;
using Rappen.XTB.Helpers.ControlItems;
using System.Xml.Serialization;

namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    [XmlInclude(typeof(OptionSetValue))]
    [XmlInclude(typeof(OptionSetValueCollection))]
    [XmlInclude(typeof(EntityReference))]
    [XmlInclude(typeof(Money))]
    public class BulkActionItem
    {
        private string entityname;
        private string attributename;
        private BulkActionAction action;

        public string EntityName
        {
            get
            {
                return Attribute?.Metadata?.EntityLogicalName ?? entityname;
            }
            set
            {
                entityname = value;
            }
        }

        public string AttributeName
        {
            get
            {
                return Attribute?.Metadata?.LogicalName ?? attributename;
            }
            set
            {
                attributename = value;
            }
        }

        internal AttributeMetadataItem Attribute { get; set; }
        public bool DontTouch { get; set; }

        public BulkActionAction Action
        {
            get => action == BulkActionAction.SetValue ? BulkActionAction.Set : action;
            set => action = value == BulkActionAction.SetValue ? BulkActionAction.Set : value;
        }

        public object Value { get; set; }
        public string StringValue { get; set; }

        public override string ToString()
        {
            var text = Attribute.GetValue();
            switch (Action)
            {
                case BulkActionAction.Set:
                    text += " = " + StringValue;
                    break;

                case BulkActionAction.Touch:
                    text += " touch";
                    break;

                case BulkActionAction.Null:
                    text += " = null";
                    break;
            }
            if (DontTouch)
            {
                text += ", don't touch";
            }
            return text;
        }

        internal void SetAttribute(IOrganizationService service, bool friendly, bool types)
        {
            if (!string.IsNullOrEmpty(entityname) && !string.IsNullOrEmpty(attributename))
            {
                Attribute = new AttributeMetadataItem(service, entityname, attributename, friendly, types);
            }
        }
    }

    public enum BulkActionAction
    {
        Set = 0,
        Calc = 3,
        Touch = 1,
        Null = 2,
        SetValue = 99
    }
}