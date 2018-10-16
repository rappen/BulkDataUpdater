using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;

namespace BulkDataUpdaterTests
{
    [TestClass]
    public class CreatePersons
    {
        private string connstr = "AuthType=Office365;Url=https://jonassandbox2.crm4.dynamics.com/;Username=somethingsomething;Password=somethingelse";

        [TestMethod]
        public void Create100Persons()
        {
            var req = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings(),
                Requests = new OrganizationRequestCollection()
            };
            for (var i = 1; i <= 100; i++)
            {
                var contact = new Entity("contact");
                contact["firstname"] = "Test" + i;
                contact["lastname"] = "Testson" + i;
                contact["importsequencenumber"] = 100;
                req.Requests.Add(new CreateRequest { Target = contact });
            }
            var client = new CrmServiceClient(connstr);
            var res = client.Execute(req) as ExecuteMultipleResponse;
        }
    }
}
