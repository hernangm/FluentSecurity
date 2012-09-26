using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using FluentSecurity.Caching;
using FluentSecurity.Policy;
using FluentSecurity.SampleApplication.Controllers;
using FluentSecurity.SampleApplication.Models;

namespace FluentSecurity.SampleApplication
{
	public static class Bootstrapper
	{
		public static ISecurityConfiguration SetupFluentSecurity()
		{
			Log.RuntimeEventListener = e => Trace.WriteLine(String.Format("{0} - {1}{2}", e.ContextId, e.Message.Replace(". ", Environment.NewLine), e.CompletedInMilliseconds != null ? " (" + e.CompletedInMilliseconds + "ms)" : ""));
			GlimpseTraceSetup.Register();
			SecurityConfigurator.Configure(configuration =>
			{
				configuration.GetAuthenticationStatusFrom(Helpers.SecurityHelper.UserIsAuthenticated);
				configuration.GetRolesFrom(Helpers.SecurityHelper.UserRoles);

				configuration.DefaultPolicyViolationHandlerIs(() => new DefaultPolicyViolationHandler());
				configuration.Advanced.ModifySecurityContext(context => context.Data.QueryString = HttpContext.Current.Request.QueryString);

				configuration.For<HomeController>().Ignore().Cache<IgnorePolicy>(Cache.PerHttpSession);
				configuration.For<HomeController>(x => x.About()).Ignore().Cache<IgnorePolicy>(Cache.DoNotCache, By.Controller);

				configuration.For<AccountController>(x => x.LogInAsAdministrator()).DenyAuthenticatedAccess();
				configuration.For<AccountController>(x => x.LogInAsPublisher()).DenyAuthenticatedAccess();
				configuration.For<AccountController>(x => x.LogOut()).DenyAnonymousAccess();

				configuration.For<ExampleController>(x => x.DenyAnonymousAccess()).DenyAnonymousAccess();
				configuration.For<ExampleController>(x => x.DenyAuthenticatedAccess()).DenyAuthenticatedAccess();

				configuration.For<ExampleController>(x => x.RequireAdministratorRole()).RequireRole(UserRole.Administrator);
				configuration.For<ExampleController>(x => x.RequirePublisherRole()).RequireRole(UserRole.Publisher);

				configuration.For<AdminController>().AddPolicy(new AdministratorPolicy());
				configuration.For<AdminController>(x => x.Delete()).DelegatePolicy("LocalOnlyPolicy",
					context => HttpContext.Current.Request.IsLocal
					);

				configuration.For<Areas.ExampleArea.Controllers.HomeController>().DenyAnonymousAccess();
				configuration.For<Areas.ExampleArea.Controllers.HomeController>(x => x.PublishersOnly()).RequireRole(UserRole.Publisher);
				configuration.For<Areas.ExampleArea.Controllers.HomeController>(x => x.AdministratorsOnly()).RequireRole(UserRole.Administrator);
			});
			return SecurityConfiguration.Current;
		}
	}
}