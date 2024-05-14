using Cinteros.XTB.BulkDataUpdater.AppCode;

namespace Cinteros.XTB.BulkDataUpdater
{
    public class Settings
    {
        public BDUJob Job { get; set; } = new BDUJob();
        public int FetchResultCount { get; set; }
        public bool Friendly { get; set; } = true;
        public UpdateAttributes UpdateAttributes { get; set; } = new UpdateAttributes();
    }

    public class UpdateAttributes
    {
        public bool Everything { get; set; } = false;
        public bool Required { get; set; } = false;
        public bool Recommended { get; set; } = false;
        public bool OnForm { get; set; } = false;
        public bool OnView { get; set; } = false;
        public bool InQuery { get; set; } = true;
        public bool CombinationAnd { get; set; } = false;
        public bool UnallowedUpdate { get; set; } = false;
        public bool ImportSequenceNumber { get; set; } = false;
    }
}