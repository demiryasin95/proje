using System.Collections.Generic;
using System.Web;
using Microsoft.Owin;

namespace StudentStudyProgram.Infrastructure
{
    public static class OwinExtensions
    {
        public static IOwinContext GetOwinContext(this HttpContextBase context)
        {
            var env = context.Items["owin.Environment"] as IDictionary<string, object>;
            return env != null ? new OwinContext(env) : new OwinContext();
        }

        public static IOwinContext GetOwinContext(this HttpRequestBase request)
        {
            var env = request.RequestContext?.HttpContext?.Items["owin.Environment"] as IDictionary<string, object>;
            return env != null ? new OwinContext(env) : new OwinContext();
        }
    }
}
