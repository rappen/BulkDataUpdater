namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    using Microsoft.Xrm.Sdk;
    using Rappen.XTB.Helpers.Interfaces;

    public class ViewItem : IComboBoxItem
    {
        private Entity view = null;

        public ViewItem(Entity View)
        {
            view = View;
        }

        public override string ToString()
        {
            return view["name"].ToString();
        }

        public Entity GetView()
        {
            return view;
        }

        public string GetValue()
        {
            return view.Id.ToString();
        }

        public string GetFetch()
        {
            if (view.Contains("fetchxml"))
            {
                return view["fetchxml"].ToString();
            }
            return "";
        }

        public string GetLayout()
        {
            if (view.Contains("layoutxml"))
            {
                return view["layoutxml"].ToString();
            }
            return "";
        }
    }
}
