using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;

namespace LogiHub.Web.Infrastructure
{
    public abstract class PagingViewModel
    {
        public int Page { get; set; }
        [Display(Name = "Elementi per pagina")]
        public int PageSize { get; set; }
        [Display(Name = "Elementi")]
        public int TotalItems { get; set; }
        public string OrderBy { get; set; }
        public bool OrderByDescending { get; set; }
        public int[] PageSizes { get; set; }

        public IEnumerable<SelectListItem> PageSizeListItems // VB: non facciamo propriet� che fanno i conti, meglio un metodo
        {
            get
            {
                return PageSizes.Select(x => new SelectListItem
                {
                    Text = x.ToString(),
                    Value = x.ToString()
                });
            }
        }

        public PagingViewModel()
        {
            Page = 1;
            SetPagingDefaults();
        }

        public virtual void SetPagingDefaults()
        {
            PageSize = 25;
            PageSizes = new int[] { 15, 25, 50, 100 };
        }

        public int TotalPages()
        {
            return (int)Math.Max(1, Math.Ceiling((double)TotalItems / PageSize));
        }

        public abstract IActionResult GetRoute();


        public string ChangePageSizePageUrl(IUrlHelper url, int pageSize)
        {
            var route = GetRoute();
            return ChangePageSizePageUrl(url, route, pageSize);
        }
        string ChangePageSizePageUrl(IUrlHelper url, IActionResult route, int pageSize)
        {
            var idx = MergeCurrentQueryString(url, route);
            idx["PageSize"] = pageSize;
            idx["Page"] = 1;

            return url.Action(idx["action"]?.ToString(), idx["controller"]?.ToString(), idx);
        }


        public string NextPageUrl(IUrlHelper url)
        {
            var route = GetRoute();
            return NextPageUrl(url, route);
        }
        string NextPageUrl(IUrlHelper url, IActionResult route)
        {
            var idx = MergeCurrentQueryString(url, route);
            idx["Page"] = Math.Min(TotalPages(), Page + 1);

            return url.Action(idx["action"]?.ToString(), idx["controller"]?.ToString(), idx);
        }
        string NextPageUrl(IUrlHelper url, Task<ActionResult> route)
        {
            return NextPageUrl(url, route.GetAwaiter().GetResult());
        }

        public string LastPageUrl(IUrlHelper url)
        {
            var route = GetRoute();
            return LastPageUrl(url, route);
        }

        string LastPageUrl(IUrlHelper url, IActionResult route)
        {
            var idx = MergeCurrentQueryString(url, route);
            idx["Page"] = TotalPages();

            return url.Action(idx["action"]?.ToString(), idx["controller"]?.ToString(), idx);
        }
        public string PrevPageUrl(IUrlHelper url)
        {
            var route = GetRoute();
            return PrevPageUrl(url, route);
        }
        string PrevPageUrl(IUrlHelper url, Task<ActionResult> route)
        {
            return PrevPageUrl(url, route.GetAwaiter().GetResult());
        }
        string PrevPageUrl(IUrlHelper url, IActionResult route)
        {
            var idx = MergeCurrentQueryString(url, route);
            idx["Page"] = Math.Max(1, Page - 1);

            return url.Action(idx["action"]?.ToString(), idx["controller"]?.ToString(), idx);
        }
        public string FirstPageUrl(IUrlHelper url)
        {
            var route = GetRoute();
            return FirstPageUrl(url, route);
        }

        string FirstPageUrl(IUrlHelper url, IActionResult route)
        {
            var idx = MergeCurrentQueryString(url, route);
            idx["Page"] = 1;

            return url.Action(idx["action"]?.ToString(), idx["controller"]?.ToString(), idx);
        }


        protected string OrderbyUrl<TModel, TProperty>(IUrlHelper url, Expression<Func<TModel, TProperty>> expression)
        {
            var propertyName = GetModelExpressionProvider(url.ActionContext.HttpContext.RequestServices).GetExpressionText(expression);
            return OrderbyUrl(url, propertyName, GetRoute());
        }

        public string OrderbyUrl(IUrlHelper url, string propertyName)
        {
            return OrderbyUrl(url, propertyName, GetRoute());
        }

        string OrderbyUrl(IUrlHelper url, string propertyName, IActionResult route)
        {
            var idx = MergeCurrentQueryString(url, route);

            if (OrderBy == propertyName)
            {
                idx["OrderByDescending"] = !OrderByDescending;
            }
            else
            {
                idx["OrderBy"] = propertyName;
                idx["OrderByDescending"] = false;
            }

            return url.Action(idx["action"]?.ToString(), idx["controller"]?.ToString(), idx);
        }

        public string ChangePageUrl(IUrlHelper url, int page)
        {
            var route = GetRoute();
            var idx = MergeCurrentQueryString(url, route);
            idx["Page"] = page;
            return url.Action(idx["action"]?.ToString(), idx["controller"]?.ToString(), idx);
        }

        protected string OrderbyCss<TModel, TProperty>(HttpContext context, Expression<Func<TModel, TProperty>> expression)
        {
            var propertyName = GetModelExpressionProvider(context.RequestServices).GetExpressionText(expression);
            return OrderbyCss(propertyName);
        }

        public string OrderbyCss(string propertyName)
        {
            var na = "fa-solid fa-sort";
            var down = "fa-solid fa-sort-down";
            var up = "fa-solid fa-sort-up";
            if (OrderBy == propertyName)
            {
                if (OrderByDescending)
                    return down;
                else
                    return up;
            }
            else
                return na;
        }

        Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpressionProvider GetModelExpressionProvider(IServiceProvider services)
        {
            return services.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpressionProvider)) as Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpressionProvider;
        }
        
        protected RouteValueDictionary MergeCurrentQueryString(IUrlHelper url, IActionResult route)
        {
            var idx = route.GetRouteValueDictionary();

            var requestQuery = url.ActionContext.HttpContext.Request.Query;

            foreach (var kv in requestQuery)
            {
                if (idx.ContainsKey(kv.Key))
                    continue;

                if (kv.Value.Count <= 1)
                    idx[kv.Key] = kv.Value.ToString();
                else
                    idx[kv.Key] = kv.Value.ToArray();
            }

            return idx;
        }

    }
}