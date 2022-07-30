using Cinteros.XTB.BulkDataUpdater.AppCode;

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
        public BDUJob Job { get; set; } = new BDUJob();
        public int FetchResultCount { get; set; }
        public bool Friendly { get; set; } = false;
    }
}