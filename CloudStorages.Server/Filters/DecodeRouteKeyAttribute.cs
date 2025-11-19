using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Web;
namespace CloudStorages.Server.Filters
{

    public class DecodeRouteKeyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionArguments.ContainsKey("fileKey"))
            {
                var key = context.ActionArguments["fileKey"]?.ToString();
                if (!string.IsNullOrEmpty(key))
                    context.ActionArguments["fileKey"] = Uri.UnescapeDataString(key);
            }

            base.OnActionExecuting(context);
        }
    }
}