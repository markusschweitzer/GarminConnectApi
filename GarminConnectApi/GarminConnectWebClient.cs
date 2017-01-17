using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GarminConnectApi
{
    internal class GarminConnectWebClient : WebClient
    {
        private CookieContainer cc = new CookieContainer();
        private string lastPage;

        public bool AllowAutoRedirect = true;

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest _request = base.GetWebRequest(address);

            if (_request is HttpWebRequest)
            {
                HttpWebRequest _httpRequest = (HttpWebRequest)_request;
                _httpRequest.AllowAutoRedirect = AllowAutoRedirect;
                _httpRequest.CookieContainer = cc;
                if (lastPage != null)
                {
                    _httpRequest.Referer = lastPage;
                }
            }
            lastPage = address.ToString();
            return _request;
        }
    }
}
