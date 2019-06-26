using System.Collections.Generic;

namespace Cinteros.XTB.BulkDataUpdater
{
    public class Settings
    {
        public bool AttributesCustom { get; set; } = true;
        public bool AttributesCustomizable { get; set; } = true;
        public bool AttributesManaged { get; set; } = true;
        public bool AttributesOnlyValidAF { get; set; } = false;
        public bool AttributesStandard { get; set; } = true;
        public bool AttributesUncustomizable { get; set; } = true;
        public bool AttributesUnmanaged { get; set; } = true;
        public List<KeyValuePair> EntityAttributes { get; set; }
        public string FetchXML { get; set; }
        public bool Friendly { get; set; } = false;
        public int DelayCallTime { get; set; } = 0;
        public int UpdateBatchSize { get; set; } = 1;
        public int DeleteBatchSize { get; set; } = 1;
    }

    public class KeyValuePair
    {
        public string key { get; set; }
        public string value { get; set; }
    }
}