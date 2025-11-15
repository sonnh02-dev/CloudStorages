using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Web;
namespace CloudStorages.Server.Filters
{

    public class DecodeRouteKeyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionArguments.ContainsKey("key"))
            {
                var key = context.ActionArguments["key"]?.ToString();
                if (!string.IsNullOrEmpty(key))
                    context.ActionArguments["key"] = Uri.UnescapeDataString(key);
            }

            base.OnActionExecuting(context);
        }
    }
}