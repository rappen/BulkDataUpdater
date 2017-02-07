using System.Collections.Generic;

namespace Cinteros.XTB.BulkDataUpdater
{
    public class Settings
    {
        public bool AttributesCustom { get; set; }
        public bool AttributesCustomizable { get; set; }
        public bool AttributesManaged { get; set; }
        public bool AttributesOnlyValidAF { get; set; }
        public bool AttributesStandard { get; set; }
        public bool AttributesUncustomizable { get; set; }
        public bool AttributesUnmanaged { get; set; }
        public List<KeyValuePair> EntityAttributes { get; set; }
        public string FetchXML { get; set; }
        public bool Friendly { get; set; }
    }

    public class KeyValuePair
    {
        public string key { get; set; }
        public string value { get; set; }
    }
}