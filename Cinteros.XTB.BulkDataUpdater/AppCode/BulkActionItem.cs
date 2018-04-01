using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System.Collections.Generic;

namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    internal class BulkActionItem
    {
        public AttributeItem Attribute { get; internal set; }
        public bool DontTouch { get; internal set; }
        public BulkActionAction Action { get; internal set; }
        public object Value { get; internal set; }
        public string StringValue { get; internal set; }

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

    internal enum BulkActionAction
    {
        SetValue = 0,
        Touch = 1,
        Null = 2
    }
}