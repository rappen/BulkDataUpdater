using System;

namespace Cinteros.XTB.BulkDataUpdater
{
    public partial class BulkDataUpdater
    {
        private static int importsequencestartsat = int.MinValue + 19710917;
        private static Random random = new Random();

        private int GetISN()
        {
            return importsequencestartsat + random.Next(1, 1000000);
        }
    }
}