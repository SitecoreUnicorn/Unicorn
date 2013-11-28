using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;
using Sitecore.Data.Items;

namespace Unicorn.Dependencies.TinyIoC
{
    public class HttpContextLifetimeProvider : TinyIoCContainer.ITinyIoCObjectLifetimeProvider
    {
        private readonly string _keyName = String.Format("TinyIoC.HttpContext.{0}", Guid.NewGuid());
	   
		[ThreadStatic]
		private static readonly Dictionary<string, object> FallbackDictionary = new Dictionary<string, object>(); 

        public object GetObject()
        {
	        if (HttpContext.Current == null)
		        return FallbackDictionary.ContainsKey(_keyName) ? FallbackDictionary[_keyName] : null;

            return HttpContext.Current.Items[_keyName];
        }

        public void SetObject(object value)
        {
	        if (HttpContext.Current == null)
	        {
		        FallbackDictionary[_keyName] = value;
		        return;
	        }

	        HttpContext.Current.Items[_keyName] = value;
        }

        public void ReleaseObject()
        {
            var item = GetObject() as IDisposable;

            if (item != null)
                item.Dispose();

            SetObject(null);
        }
    }

    public static class TinyIoCAspNetExtensions
    {
        public static TinyIoCContainer.RegisterOptions AsPerRequestSingleton(this TinyIoCContainer.RegisterOptions registerOptions)
        {
            return TinyIoCContainer.RegisterOptions.ToCustomLifetimeManager(registerOptions, new HttpContextLifetimeProvider(), "per request singleton");
        }
    }
}
