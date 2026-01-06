using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using LogiHub.Web.Infrastructure;
using LogiHub.Web.Features.Breadcrumb;

namespace LogiHub.Web.Areas;

[Authorize]
[Alerts]
[ModelStateToTempData]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public partial class AuthenticatedBaseController : Controller
{
    public AuthenticatedBaseController()
    {
    }

    protected IdentitaViewModel Identita
    {
        get { return (IdentitaViewModel)ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY]; }
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        try
        {
            if (context.HttpContext != null && context.HttpContext.User != null &&
                context.HttpContext.User.Identity.IsAuthenticated)
            {
                ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
                {
                    EmailUtenteCorrente = context.HttpContext.User.Claims.Where(x => x.Type == ClaimTypes.Email).First()
                        .Value
                };
            }
            else
            {
                HttpContext.SignOutAsync();
                this.SignOut();

                context.Result = new RedirectResult(context.HttpContext.Request.GetEncodedUrl());
                Alerts.AddError(this, "L'utente non possiede i diritti per visualizzare la risorsa richiesta");
            }

            base.OnActionExecuting(context);
        }
        catch (Exception)
        {
            throw;
        }
    }
    
    protected void SetBreadcrumb(params (string Text, string Url)[] steps)
    {
        var crumbs = new List<BreadcrumbItem>();
        

        if (steps != null)
        {
            foreach (var step in steps)
            {
                crumbs.Add(new BreadcrumbItem
                {
                    Text = step.Text,
                    Url = step.Url,
                    IsActive = false
                });
            }
        }
        
        if (crumbs.Any())
        {
            var last = crumbs.Last();
            last.IsActive = true;
            last.Url = null;
        }
        
        ViewData["Breadcrumbs"] = crumbs;
    }
}