using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace GarminConnectApi
{
    public class GarminConnection : IDisposable
    {
        private const string _loginUrl = "https://sso.garmin.com/sso/login?service=http%3A%2F%2Fconnect.garmin.com%2Fpost-auth%2Flogin&webhost=olaxpw-connect07.garmin.com&source=http%3A%2F%2Fconnect.garmin.com%2Fde-DE%2Fsignin&redirectAfterAccountLoginUrl=http%3A%2F%2Fconnect.garmin.com%2Fpost-auth%2Flogin&redirectAfterAccountCreationUrl=http%3A%2F%2Fconnect.garmin.com%2Fpost-auth%2Flogin&gauthHost=https%3A%2F%2Fsso.garmin.com%2Fsso&locale=de&id=gauth-widget&cssUrl=https%3A%2F%2Fstatic.garmincdn.com%2Fcom.garmin.connect%2Fui%2Fsrc-css%2Fgauth-custom.css&clientId=GarminConnect&rememberMeShown=true&rememberMeChecked=false&createAccountShown=true&openCreateAccount=false&usernameShown=true&displayNameShown=false&consumeServiceTicket=false&initialFocus=true&embedWidget=false";
        private const string _responseUrlSplit = "  var response_url                 = '";

        private GarminConnectWebClient _webClient;

        private Dictionary<string, object> _profileProperties;

        public string DisplayName
        {
            get
            {
                return GetProfilePropertyOrDefault("displayName", "");
            }
        }

        public int ProfileId
        {
            get
            {
                return GetProfilePropertyOrDefault("profileId", -1);
            }
        }

        public string UserName
        {
            get
            {
                return GetProfilePropertyOrDefault("userName", "");
            }
        }

        public bool Authenticated
        {
            get
            {
                return !string.IsNullOrEmpty(DisplayName);
            }
        }

        public GarminConnection()
        {
            _webClient = new GarminConnectWebClient();
        }


        public bool Authenticate(string username, string password, string referer)
        {
            _webClient.AllowAutoRedirect = false;

            //0. seems garmin needs any kind of referer
            _webClient.Headers.Add("referer", referer);

            //1. Get flowExecutionKey
            string flowExecutionKey = GetFlowExecutionKey();

            //2. Post Data
            var formParams = new NameValueCollection();
            formParams.Add("_eventId", "submit");
            formParams.Add("displayNameRequired", "false");
            formParams.Add("embed", "true");
            formParams.Add("lt", flowExecutionKey);
            formParams.Add("password", password);
            formParams.Add("username", username);

            var response = _webClient.UploadValues(_loginUrl, "POST", formParams);

            //3. Login postback
            var responseString = Encoding.Default.GetString(response);

            int responseUrlIndexStart = responseString.IndexOf(_responseUrlSplit) + _responseUrlSplit.Length;
            int responseUrlIndexEnd = responseString.IndexOf('\'', responseUrlIndexStart);

            string responseUrl = responseString.Substring(responseUrlIndexStart, responseUrlIndexEnd - responseUrlIndexStart);
            responseUrl = responseUrl.Replace("\\/", "/");

            var dat = _webClient.DownloadString(responseUrl);

            //4. There need to be 8 redirects to get the right cookie value
            int curen_red = 1;
            int max_red = 9;
            while (true)
            {
                var x = _webClient.DownloadString(_webClient.ResponseHeaders["Location"]);
                curen_red += 1;
                if (curen_red > max_red)
                {
                    GetProfileProperties(x); //last redirect will be the modern start page with the profile loaded
                    break;
                }
            }
            _webClient.AllowAutoRedirect = true;


            //5. test 
            return !string.IsNullOrEmpty(DisplayName);
        }

        public string DownloadString(string url)
        {
            return _webClient.DownloadString(url);
        }

        public void DownloadFile(string url, string fileName)
        {
            _webClient.DownloadFile(url, fileName);
        }

        public WebClient GetAuthenticatedWebClient()
        {
            if (Authenticated)
            {
                return _webClient;
            }
            return null;
        }



        private void GetProfileProperties(string data)
        {
            int indexStart = data.IndexOf("VIEWER_SOCIAL_PROFILE = JSON.parse(\"") + "VIEWER_SOCIAL_PROFILE = JSON.parse(\"".Length;
            int indexEnd = data.IndexOf("\");", indexStart);

            string jsonData = data.Substring(indexStart, indexEnd - indexStart).Replace("\\", "");
            JavaScriptSerializer js = new JavaScriptSerializer();
            _profileProperties = (Dictionary<string, object>)js.DeserializeObject(jsonData);
        }

        private string GetFlowExecutionKey()
        {
            var result = _webClient.DownloadString(_loginUrl);
            string[] splt1 = Regex.Split(result, "<!-- flowExecutionKey: ");
            string[] splt2 = Regex.Split(splt1[1], "] -->");
            return splt2[0].Replace("[", "");
        }

        private T GetProfilePropertyOrDefault<T>(string key, T defaultVal)
        {
            if (_profileProperties != null && _profileProperties.Count > 0 && _profileProperties.ContainsKey(key))
            {
                return (T)_profileProperties[key];
            }
            else
            {
                return defaultVal;
            }
        }



        public void Dispose()
        {
            _webClient.Dispose();
            _webClient = null;
        }
    }
}
