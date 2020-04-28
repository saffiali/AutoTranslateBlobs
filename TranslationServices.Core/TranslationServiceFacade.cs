﻿// // ----------------------------------------------------------------------
// // <copyright file="TranslationServiceFacade.cs" company="Microsoft Corporation">
// // Copyright (c) Microsoft Corporation.
// // All rights reserved.
// // THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// // KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// // IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// // PARTICULAR PURPOSE.
// // </copyright>
// // ----------------------------------------------------------------------
// // <summary>TranslationServiceFacade.cs</summary>
// // ----------------------------------------------------------------------

#region Usings
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
#endregion

namespace TranslationAssistant.TranslationServices.Core
{

    public static partial class TranslationServiceFacade
    {
        #region Static Fields

        private const int MillisecondsTimeout = 100;

        public static event EventHandler RetryingEvent;

        private const int maxrequestsize = 5000;   //service size is 5000
        private const int maxelements = 100;
        public static string CategoryID { get; set; }
        public static string AppId { get; set; }
        public static string AdvCategoryId { get; set; }
        public static bool UseAdvancedSettings { get; set; }
        public static bool UseAzureGovernment { get; set; }
        /// <summary>
        /// Holds the setting whether to use a container offline
        /// </summary>
        public static bool UseCustomEndpoint { get; set; }
        /// <summary>
        /// Holds the value of the custom endpoint, the container
        /// </summary>
        public static string CustomEndpointUrl { get; set; }
        public static bool ShowExperimental { get; set; }

        /// <summary>
        /// Holds the Azure subscription key
        /// </summary>
        public static string AzureKey { get; set; }
        /// <summary>
        /// Allows to set the file name to save the TMX under.
        /// No effect if CreateTMXOnTranslate is not set.
        /// </summary>
        public static string TmxFileName { get; set; } = "_DocumentTranslator.TMX";
        /// <summary>
        /// Create a TMX file containing source and target while translating. 
        /// </summary>
        public static bool CreateTMXOnTranslate { get; set; } = false;

        /// <summary>
        /// End point address for the Translator API
        /// </summary>
        public static string EndPointAddress { get; set; } = "https://api.cognitive.microsofttranslator.com";

        /// <summary>
        /// End point address for V3 of the Translator API
        /// </summary>
        public static string EndPointAddressV3Public { get; set; } = "https://api.cognitive.microsofttranslator.com";
        public static string EndPointAddressV3Gov { get; set; } = "https://api.cognitive.microsofttranslator.us";
        public static Dictionary<string, string> AvailableLanguages { get; } = new Dictionary<string, string>();
        public static int Maxrequestsize { get => maxrequestsize; }
        public static int Maxelements { get => maxelements; }

        public enum ContentType { plain, HTML };

        /// <summary>
        /// Authentication Service URL
        /// </summary>
        private static readonly Uri AuthServiceUrlPublic = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");
        private static readonly Uri AuthServiceUrlGov = new Uri("https://virginia.api.cognitive.microsoft.us/sts/v1.0/issueToken");

        private enum AuthMode { Azure, AppId, Container, Disconnected };
        private static AuthMode authMode = AuthMode.Disconnected;
        private static string appid = null;

        private static readonly List<string> autoDetectStrings = new List<string>() { "auto-detect", "détection automatique" };

        private static bool IsInitialized = false;
        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Detect the languages of the input
        /// </summary>
        /// <param name="input">Input string to detect the language of</param>
        /// <returns></returns>
        private static async Task<string> DetectInternalAsync(string input, bool pretty = false)
        {
            string uri = (UseAzureGovernment ? EndPointAddressV3Gov : EndPointAddressV3Public) + "/detect?api-version=3.0";
            string result = String.Empty;
            object[] body = new object[] { new { Text = input } };
            using (HttpClient client = new HttpClient())
            using (HttpRequestMessage request = new HttpRequestMessage())
            {
                client.Timeout = TimeSpan.FromSeconds(2);
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                string requestBody = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", AzureKey);
                HttpResponseMessage response = new HttpResponseMessage();
                try
                {
                    response = await client.SendAsync(request).ConfigureAwait(false);
                }
                catch (HttpRequestException e)
                {
                    Debug.WriteLine("Received exception {0}", e.Message);
                    return null;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string detectResult= await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (pretty)
                    {
                        using (var stringReader = new StringReader(detectResult))
                        using (var stringWriter = new StringWriter())
                        {
                            JsonTextReader jsonReader = new JsonTextReader(stringReader);
                            JsonTextWriter jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                            jsonWriter.WriteToken(jsonReader);
                            return stringWriter.ToString();
                        }
                    }
                    else
                    {
                        return detectResult;
                    }
                       
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Detects the most likely language of the input.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="simple"></param>
        /// <returns></returns>
        public static async Task<string> DetectAsync(string input, bool simple = true)
        {
            if (UseCustomEndpoint) return null;
            if (simple)
            {
                DetectResult[] detectResults = await DetectAsync(input).ConfigureAwait(false);
                return detectResults[0].Language;
            }
            else
            {
                return await DetectInternalAsync(input, true).ConfigureAwait(false);
            }
        }

        public static async Task<DetectResult[]> DetectAsync(string input)
        {
            if (UseCustomEndpoint) return null;

            string result = await DetectInternalAsync(input).ConfigureAwait(false);
            DetectResult[] deserializedOutput = JsonConvert.DeserializeObject<DetectResult[]>(result);
            return deserializedOutput;
        }

        public class DetectResult
        {
            public string Language { get; set; }
            public float Score { get; set; }
            public bool IsTranslationSupported { get; set; }
            public bool IsTransliterationSupported { get; set; }
            public AltTranslations[] Alternatives { get; set; }
        }
        public class AltTranslations
        {
            public string Language { get; set; }
            public float Score { get; set; }
            public bool IsTranslationSupported { get; set; }
            public bool IsTransliterationSupported { get; set; }
        }


        /// <summary>
        /// Check if the Translation service is ready to use, with a valid Azure key
        /// </summary>
        /// <returns>true if ready, false if not</returns>
        public static async Task<bool> IsTranslationServiceReadyAsync()
        {
            if (UseCustomEndpoint)
            {
                return await ContainerStatus().ConfigureAwait(false);
            }
            else
            {
                string detectedlanguage = await DetectInternalAsync("Test").ConfigureAwait(false);
                if (detectedlanguage == null) return false;
                return true;
            }
        }

        /// <summary>
        /// Test whether the translation service credentials are valid by performing a simple function and checking whether it succeeds.
        /// Synchronous version of IsTranslationServceReadyAsync().
        /// </summary>
        /// <returns>Whether the translation servoce is ready to translate</returns>
        public static bool IsTranslationServiceReady()
        {
            Task<bool> task = Task.Run(async () => await IsTranslationServiceReadyAsync().ConfigureAwait(false));
            return task.Result;
        }

        /// <summary>
        /// Test if a given category value is a valid category in the system.
        /// Works across V2 and V3 of the API.
        /// </summary>
        /// <param name="category">Category ID</param>
        /// <returns>True if the category is valid</returns>
        public static async Task<bool> IsCategoryValidAsync(string category)
        {
            if (string.IsNullOrEmpty(category)) return true;
            if (category.ToLowerInvariant() == "general") return true;
            if (category.ToLowerInvariant() == "generalnn") return true;
            if (category.ToLowerInvariant() == "tech") return true;

            return await IsCategoryValidV3Async(category).ConfigureAwait(false);
        }

        private static async Task<bool> IsCategoryValidV3Async(string category)
        {
            bool returnvalue = true;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    string[] teststring = { "Test" };
                    Task<string[]> translateTask = TranslateV3Async(teststring, "en", "he", category);
                    await translateTask.ConfigureAwait(false);
                    if (translateTask.Result == null) return false; else return true;
                }
                catch (Exception e)
                {
                    string error = e.Message;
                    returnvalue = false;
                    Thread.Sleep(1000);
                    continue;
                }
            }
            return returnvalue;
        }

        
        /// <summary>
        /// Call once to initialize the static variables
        /// </summary>
        public static void Initialize(bool force = false)
        {
            if (IsInitialized && !force) return;
            //LoadCredentials();
            if (String.IsNullOrEmpty(AzureKey))
            {
                if (UseCustomEndpoint == false)
                {
                    Exception CredentialsMissingException = new CredentialsMissingException("Credentials Missing Exception");
                    throw CredentialsMissingException;
                }
                authMode = AuthMode.Disconnected;
            }

            if (UseCustomEndpoint)
            {
                authMode = AuthMode.Container;
            }
            //Inspect the given Azure Key to see if this is host with appid auth
            string[] AuthComponents = AzureKey.Split('?');
            if (AuthComponents.Length > 1)
            {
                EndPointAddress = AuthComponents[0];
                string[] appidComponents = AuthComponents[1].ToUpperInvariant().Split('=');
                if (appidComponents[0] == "APPID")
                {
                    authMode = AuthMode.AppId;
                    appid = appidComponents[1];
                }
                else return;
            }
            GetLanguages();
            IsInitialized = true;
        }


        private static async void GetLanguages()
        {
            AvailableLanguages.Clear();
            if (UseCustomEndpoint)
            {
                ContainerGetLanguages();
                return;
            }
            string uri = (UseAzureGovernment ? EndPointAddressV3Gov : EndPointAddressV3Public) + "/languages?api-version=3.0&scope=translation";
            if (ShowExperimental) uri += "&flight=experimental";

            try
            {
                using (HttpClient client = new HttpClient())
                using (HttpRequestMessage request = new HttpRequestMessage())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    request.Method = HttpMethod.Get;
                    request.RequestUri = new Uri(uri);
                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                    string jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(jsonResponse);
                        var languages = result["translation"];

                        string[] languagecodes = languages.Keys.ToArray();
                        foreach (var kv in languages)
                        {
                            AvailableLanguages.Add(kv.Key, kv.Value["name"]);
                        }
                    }
                }
            }
            catch
            {
                authMode = AuthMode.Disconnected;
                AvailableLanguages.Clear();
                return;
            }
            return;
        }


        /// <summary>
        /// Loads credentials from settings file.
        /// Doesn't need to be public, because it is called during Initialize();
        /// </summary>
        private static void LoadCredentials()
        {
            AzureKey = LoadSaveCredentials.GetEnvironmentVariable("AzureKey");
            CategoryID = LoadSaveCredentials.GetEnvironmentVariable("CategoryID");
            AppId = LoadSaveCredentials.GetEnvironmentVariable("AppId");
            UseAdvancedSettings = Convert.ToBoolean(LoadSaveCredentials.GetEnvironmentVariable("UseAdvancedSettings")) ;
            AdvCategoryId = LoadSaveCredentials.GetEnvironmentVariable("Adv_CategoryID");
            UseAzureGovernment = Convert.ToBoolean(LoadSaveCredentials.GetEnvironmentVariable("UseAzureGovernment"));
            UseCustomEndpoint = Convert.ToBoolean(LoadSaveCredentials.GetEnvironmentVariable("UseCustomEndpoint"));
            CustomEndpointUrl = LoadSaveCredentials.GetEnvironmentVariable("CustomEndpointUrl");
            ShowExperimental = Convert.ToBoolean(LoadSaveCredentials.GetEnvironmentVariable("ShowExperimental"));
        }

        public static void LoadCredentials(string azureKey, string categoryID, string appId="")
        {
            AzureKey = azureKey;
            CategoryID = categoryID;
            AppId = appId;
            //UseAdvancedSettings = Convert.ToBoolean(LoadSaveCredentials.GetEnvironmentVariable("UseAdvancedSettings"));
            //AdvCategoryId = LoadSaveCredentials.GetEnvironmentVariable("Adv_CategoryID");
            //UseAzureGovernment = Convert.ToBoolean(LoadSaveCredentials.GetEnvironmentVariable("UseAzureGovernment"));
            //UseCustomEndpoint = Convert.ToBoolean(LoadSaveCredentials.GetEnvironmentVariable("UseCustomEndpoint"));
            //CustomEndpointUrl = LoadSaveCredentials.GetEnvironmentVariable("CustomEndpointUrl");
            //ShowExperimental = Convert.ToBoolean(LoadSaveCredentials.GetEnvironmentVariable("ShowExperimental"));
        }

        /// <summary>
        /// Saves credentials Azure Key and categoryID to the personalized settings file.
        /// </summary>
        public static void SaveCredentials()
        {
            //Properties.Settings.Default.AzureKey = AzureKey;
            //Properties.Settings.Default.CategoryID = CategoryID;
            //Properties.Settings.Default.AppId = AppId;
            //Properties.Settings.Default.UseAdvancedSettings = UseAdvancedSettings;
            //Properties.Settings.Default.Adv_CategoryID = AdvCategoryId;
            //Properties.Settings.Default.UseAzureGovernment = UseAzureGovernment;
            //Properties.Settings.Default.UseCustomEndpoint = UseCustomEndpoint;
            //Properties.Settings.Default.CustomEndpointUrl = CustomEndpointUrl;
            //Properties.Settings.Default.ShowExperimental = ShowExperimental;
            //Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Takes a single language name and returns the matching language code. OK to pass a language code.
        /// </summary>
        /// <param name="languagename"></param>
        /// <returns></returns>
        public static string LanguageNameToLanguageCode(string languagename)
        {
            if (AvailableLanguages.ContainsKey(languagename))
            {
                return languagename;
            }
            else if (AvailableLanguages.ContainsValue(languagename))
            {
                return AvailableLanguages.First(t => t.Value == languagename).Key;
            }
            else
            {
                return String.Empty;
            }
        }

        public static string LanguageCodeToLanguageName(string languagecode)
        {
            switch (languagecode.ToLowerInvariant())
            {
                case "zh-hans":
                case "zh-chs":
                case "zh-cn":
                    languagecode = "zh-Hans";
                    break;
                case "zh-hant":
                case "zh-cht":
                case "zh-tw":
                    languagecode = "zh-Hant";
                    break;
                case "zh-hk":
                    languagecode = "yue";
                    break;
            }
            if (AvailableLanguages.ContainsValue(languagecode))
            {
                return languagecode;
            }
            else if (AvailableLanguages.ContainsKey(languagecode))
            {
                return AvailableLanguages.First(t => t.Key == languagecode).Value;
            }
            else
            {
                throw new ArgumentException(String.Format("LanguageCodeToLanguageName: Language code {0} not found.", languagecode));
            }
        }


        /// <summary>
        /// Translates a string
        /// </summary>
        /// <param name="text">String to translate</param>
        /// <param name="from">From language</param>
        /// <param name="to">To language</param>
        /// <param name="contentType">Optional: Content Type: 0=plain text (default), 1=HTML</param>
        /// <returns></returns>
        public static string TranslateString(string text, string from, string to, ContentType contentType = ContentType.plain)
        {
            string[] texts = new string[1];
            texts[0] = text;
            string[] results = TranslateArray(texts, from, to, contentType);
            return results[0];
        }


        /// <summary>
        /// Translates an array of strings from the from language code to the to language code.
        /// From language code can stay empty, in that case the source language is auto-detected, across all elements of the array together.
        /// </summary>
        /// <param name="texts">Array of strings to translate</param>
        /// <param name="from">From language code. May be empty</param>
        /// <param name="to">To language code. Must be a valid language</param>
        /// <param name="contentType">Enum: ContentType plain or HTML depending on the type of string</param>
        /// <returns></returns>
        public static string[] TranslateArray(string[] texts, string from, string to, ContentType contentType = ContentType.plain)
        {
            string fromCode = string.Empty;
            string toCode = string.Empty;

            if (string.IsNullOrEmpty(from) || autoDetectStrings.Contains(from.ToLower(CultureInfo.InvariantCulture)))
            {
                fromCode = string.Empty;
            }
            else
            {
                try { fromCode = AvailableLanguages.First(t => t.Value == from).Key; }
                catch { fromCode = from; }
            }

            toCode = LanguageNameToLanguageCode(to);

            string[] result = TranslateV3Async(texts, fromCode, toCode, CategoryID, contentType).Result;
            return result;
        }

        /// <summary>
        /// Split a string > than <see cref="Maxrequestsize"/> into a list of smaller strings, at the appropriate sentence breaks. 
        /// </summary>
        /// <param name="text">The text to split.</param>
        /// <param name="languagecode">The language code to apply.</param>
        /// <returns>List of strings, each one smaller than maxrequestsize</returns>
        private static async Task<List<string>> SplitStringAsync(string text, string languagecode)
        {
            List<string> result = new List<string>();
            int previousboundary = 0;
            if (text.Length <= Maxrequestsize)
            {
                result.Add(text);
            }
            else
            {
                while (previousboundary <= text.Length)
                {
                    int boundary = await LastSentenceBreak(text.Substring(previousboundary), languagecode).ConfigureAwait(false);
                    if (boundary == 0) break;
                    result.Add(text.Substring(previousboundary, boundary));
                    previousboundary += boundary;
                }
                result.Add(text.Substring(previousboundary));
            }
            return result;
        }


        /// <summary>
        /// Returns the last sentence break in the text.
        /// </summary>
        /// <param name="text">The original text</param>
        /// <param name="languagecode">A language code</param>
        /// <returns>The offset of the last sentence break, from the beginning of the text.</returns>
        private static async Task<int> LastSentenceBreak(string text, string languagecode)
        {
            int sum = 0;
            List<int> breakSentenceResult = await BreakSentencesAsync(text, languagecode).ConfigureAwait(false);
            for (int i = 0; i < breakSentenceResult.Count-1; i++) sum += breakSentenceResult[i];
            return sum;
        }


        private static bool IsCustomCategory(string categoryID)
        {
            if (string.IsNullOrEmpty(categoryID)) return false;
            string category = categoryID.ToLowerInvariant();
            if (category == "general") return false;
            if (category == "generalnn") return false;
            return true;
        }

        /// <summary>
        /// Translates a string.
        /// </summary>
        /// <param name="text">Test to translate</param>
        /// <param name="from">From languagecode</param>
        /// <param name="to">To languagecode</param>
        /// <param name="category">Category ID</param>
        /// <param name="contentType">Plain text or HTML. Default is plain text.</param>
        /// 
        /// <returns></returns>
        public static async Task<string> TranslateStringAsync(string text, string from, string to, string category, ContentType contentType = ContentType.plain)
        {
            string[] vs = new string[1];
            vs[0] = text;
            Task<string[]> task = TranslateV3Async(vs, from, to, CategoryID, contentType);
            await task.ConfigureAwait(false);
            try
            {
                return task.Result[0];
            }
            catch (System.IndexOutOfRangeException)
            {
                //A translate call with an expired subscription causes a null return
                Exception CredentialsMissingException = new CredentialsMissingException("CredentialsMissingException");
                throw CredentialsMissingException;
            }
        }



        // Used in the BreakSentences method.
        private class BreakSentenceResult
        {
            public int[] SentLen { get; set; }
            public DetectedLanguage DetectedLanguage { get; set; }
        }

        private class DetectedLanguage
        {
            public string Language { get; set; }
            public float Score { get; set; }
        }

        public static async Task<string> Dictionary(string text, string from, string to)
        {
            string path = "/dictionary/lookup?api-version=3.0";
            string params_ = "&from=" + from + "&to=" + to;
            string uri = EndPointAddressV3Public + path + params_;
            object[] body = new object[] { new { Text = text } };
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", AzureKey);
                var response = client.SendAsync(request).Result;
                var jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return jsonResponse;
            }
        }



        /// <summary>
        /// Breaks string into sentences. The string will be cut off at maxrequestsize. 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="language"></param>
        /// <returns>List of integers denoting the offset of the sentence boundaries</returns>
        public static async Task<List<int>> BreakSentencesAsync(string text, string languagecode)
        {
            if (UseCustomEndpoint)
            {
                throw new Exception("ContainerBreakSentences");
            }
            if (String.IsNullOrEmpty(text) || String.IsNullOrWhiteSpace(text)) return null;
            string path = "/breaksentence?api-version=3.0";
            string params_ = "&language=" + languagecode;
            string uri = EndPointAddressV3Public + path + params_;
            if (UseAzureGovernment) uri = EndPointAddressV3Gov + path + params_;
            object[] body = new object[] { new { Text = text.Substring(0, (text.Length < Maxrequestsize) ? text.Length : Maxrequestsize) } };
            string requestBody = JsonConvert.SerializeObject(body);
            List<int> resultList = new List<int>();

            using (HttpClient client = new HttpClient())
            using (HttpRequestMessage request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", AzureKey);
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                BreakSentenceResult[] deserializedOutput = JsonConvert.DeserializeObject<BreakSentenceResult[]>(result);
                foreach (BreakSentenceResult o in deserializedOutput)
                {
                    //Console.WriteLine("The detected language is '{0}'. Confidence is: {1}.", o.DetectedLanguage.Language, o.DetectedLanguage.Score);
                    //Console.WriteLine("The first sentence length is: {0}", o.SentLen[0]);
                    resultList = o.SentLen.ToList();
                }
            }
            return resultList;
        }

        /// <summary>
        /// Translate an array of texts. An element may be larger than <see cref="Maxrequestsize"/>.
        /// </summary>
        /// <param name="texts">Array of text elements</param>
        /// <param name="from">From language</param>
        /// <param name="to">To language</param>
        /// <param name="category">Category ID from Custom Translator</param>
        /// <param name="contentType">Plain text or HTML</param>
        /// <param name="retrycount">How many times to retry</param>
        /// <returns></returns>
        private static async Task<string[]> TranslateV3Async(string[] texts, string from, string to, string category, ContentType contentType = ContentType.plain, int retrycount = 3)
        {
            if (from == to) return texts;
            bool translateindividually = false;
            foreach(string text in texts)
            {
                if (text.Length >= Maxrequestsize) translateindividually = true;
            }
            if (translateindividually)
            {
                List<string> resultlist = new List<string>();
                foreach (string text in texts)
                {
                    List<string> splitstring = await SplitStringAsync(text, from).ConfigureAwait(false);
                    string linetranslation = string.Empty;
                    foreach (string innertext in splitstring)
                    {
                        string innertranslation = await TranslateStringAsync(innertext, from, to, category, contentType).ConfigureAwait(false);
                        linetranslation += innertranslation;
                    }
                    resultlist.Add(linetranslation);
                }
                return resultlist.ToArray();
            }
            else
            {
                return await TranslateV3AsyncInternal(texts, from, to, category, contentType).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Raw function to translate an array of strings. Does not allow elements to be larger than <see cref="Maxrequestsize"/>.
        /// </summary>
        /// <param name="texts"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="category"></param>
        /// <param name="contentType"></param>
        /// <param name="retrycount"></param>
        /// <returns></returns>
        private static async Task<string[]> TranslateV3AsyncInternal(
            string[] texts,
            string from,
            string to,
            string category,
            ContentType contentType,
            int retrycount = 3)
        {
            if (UseCustomEndpoint)
            {
                List<string> results = new List<string>();
                foreach (string text in texts)
                {
                    string translationresult = await ContainerTranslateTextAsync(text, from, to).ConfigureAwait(false);
                    results.Add(translationresult);
                }
                return results.ToArray();
            }
            else
            {
                string path = "/translate?api-version=3.0";
                if (ShowExperimental) path += "&flight=experimental";
                string params_ = "&from=" + from + "&to=" + to;
                string thiscategory = category;
                if (String.IsNullOrEmpty(category))
                {
                    thiscategory = null;
                }
                else
                {
                    if (thiscategory == "generalnn") thiscategory = null;
                    if (thiscategory == "general") thiscategory = null;
                }
                if (thiscategory != null) params_ += "&category=" + System.Web.HttpUtility.UrlEncode(category);
                if (contentType == ContentType.HTML) params_ += "&textType=HTML";
                string uri = EndPointAddressV3Public + path + params_;
                if (UseAzureGovernment) uri = EndPointAddressV3Gov + path + params_;


                ArrayList requestAL = new ArrayList();
                foreach (string text in texts)
                {
                    requestAL.Add(new { Text = text });
                }
                string requestJson = JsonConvert.SerializeObject(requestAL);

                IList<string> resultList = new List<string>();
                while (retrycount > 0)
                {
                    var client = new HttpClient();
                    var request = new HttpRequestMessage();
                    client.Timeout = TimeSpan.FromSeconds(20);
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(uri);
                    request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", AzureKey);
                    HttpResponseMessage response = new HttpResponseMessage();
                    response.StatusCode = System.Net.HttpStatusCode.RequestTimeout;
                    string responseBody = string.Empty;

                    try
                    {
                        response = await client.SendAsync(request).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        response.StatusCode = System.Net.HttpStatusCode.RequestTimeout;
                        Thread.Sleep(MillisecondsTimeout);
                        request.Dispose(); client.Dispose(); response.Dispose();
                        continue;
                    }
                    int status = (int)response.StatusCode;
                    switch (status)
                    {
                        case 200:
                            break; ;
                        case 408:       //Custom system is being loaded
                            System.Diagnostics.Debug.WriteLine("Retry #" + retrycount + " Response: " + (int)response.StatusCode);
                            Thread.Sleep(MillisecondsTimeout * 10);
                            RetryingEvent?.Invoke(null, EventArgs.Empty);
                            request.Dispose(); client.Dispose(); response.Dispose();
                            continue;
                        case 429:
                        case 500:
                        case 503:       //translate the array one element at a time
                            if (texts.Length > 1)
                            {
                                for (int i = 0; i < texts.Length; i++)
                                {
                                    try
                                    {
                                        string[] totranslate = new string[1];
                                        totranslate[0] = texts[i];
                                        string[] result = new string[1];
                                        result = await TranslateV3AsyncInternal(totranslate, from, to, category, contentType, 2).ConfigureAwait(false);
                                        resultList.Add(result[0]);
                                    }
                                    catch
                                    {
                                        System.Diagnostics.Debug.WriteLine("Failed to translate: {0}\n", texts[i]);
                                        resultList.Add(texts[i]);
                                    }
                                }
                                return resultList.ToArray();
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Retry #" + retrycount + " Response: " + (int)response.StatusCode);
                                Thread.Sleep(MillisecondsTimeout);
                                request.Dispose(); client.Dispose(); response.Dispose();
                                continue;
                            }
                        default:
                            var errorstring = "ERROR " + response.StatusCode + "\n" + JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseBody), Formatting.Indented);
                            Thread.Sleep(MillisecondsTimeout * 5);
                            retrycount--;
                            request.Dispose(); client.Dispose(); response.Dispose();
                            continue;
                    }
                    responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    request.Dispose(); client.Dispose(); response.Dispose();
                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        JArray jaresult;
                        try
                        {
                            jaresult = JArray.Parse(responseBody);
                        }
                        catch (Exception)
                        {
                            System.Diagnostics.Debug.WriteLine(responseBody);
                            throw;
                        }
                        foreach (JObject result in jaresult)
                        {
                            string txt = (string)result.SelectToken("translations[0].text");
                            resultList.Add(txt);
                        }
                    }
                    break;
                }

                return resultList.ToArray();
            }
        }

        private static List<string> ParseJsonResult(string responseBody)
        {
            List<string> resultList = new List<string>();
            JArray jaresult;
            try
            {
                jaresult = JArray.Parse(responseBody);
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine(responseBody);
                throw;
            }
            foreach (JObject result in jaresult)
            {
                string txt = (string)result.SelectToken("translations[0].text");
                resultList.Add(txt);
            }
            return resultList;
        }


        private static void WriteToTmx(string[] texts, string[] res, string from, string to, string comment)
        {
            TranslationMemory TM = new TranslationMemory();
            TranslationUnit TU = new TranslationUnit();
            TM.sourceLangID = from;
            TM.targetLangID = to;
            for (int i=0; i<texts.Length; i++)
            {
                TU.strSource = texts[i];
                TU.strTarget = res[i];
                TU.user = "DocumentTranslator";
                TU.status = TUStatus.good;
                TU.comment = comment;
                TM.Add(TU);
            }
            TM.WriteToTmx(TmxFileName);
            return;
        }



        #endregion
    }
}