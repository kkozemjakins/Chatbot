using System.Web;
using System.Web.Mvc;

namespace Psychological_Support_Chatbot
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
