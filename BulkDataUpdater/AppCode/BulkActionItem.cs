using Rappen.XTB.Helpers.ControlItems;

namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    public class BulkActionItem
    {
        private string attributename;

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
        public BulkActionAction Action { get; set; }
        public object Value { get; set; }
        public string StringValue { get; set; }

        public override string ToString()
        {
            var text = Attribute.GetValue();
            switch (Action)
            {
                case BulkActionAction.SetValue:
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
    }

    public enum BulkActionAction
    {
        SetValue = 0,
        Calc = 3,
        Touch = 1,
        Null = 2
    }
}