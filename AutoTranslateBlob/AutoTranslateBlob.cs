using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Host;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using HtmlAgilityPack;
using TranslationAssistant.TranslationServices.Core;
using TranslationAssistant.Business;

namespace Saffi.Translate
{
    public static class AutoTranslateBlob
    {
        /// <summary>
        /// //Translate HTML and Text documents. Then save it in a different container.
        /// </summary>
        /// <param name="InputStream"></param>
        /// <param name="OutputText"></param>
        /// <param name="name"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("AutoTranslateBlob")]
        public static async Task Run(
            [BlobTrigger("to-be-translated/{name}", Connection = "AzureWebJobsStorage")]Stream InputStream,
            [Blob("translated/{name}", FileAccess.Write, Connection = "AzureWebJobsStorage")] TextWriter OutputText, string name, ILogger log)
        
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            
            string TranslatedContent = string.Empty;
            try
            {
                log.LogInformation($"STARTED: AutoTranslateBlob function for blob Name:{name} of Size: {InputStream.Length} Bytes");

               

                OutputText.WriteLine("");

                //Get Environment Variables
                string ToLang = GetEnvironmentVariable("ToLang");
                string FromLang = GetEnvironmentVariable("FromLang");
                string AzureKey = GetEnvironmentVariable("AzureTranslateKey");
                string CategoryID = GetEnvironmentVariable("CategoryID");
                string FileExtension = name.Split('.').Last().ToLower();

                TranslationServiceFacade.LoadCredentials(AzureKey, CategoryID);
                TranslationServiceFacade.Initialize(true);

                //ReadFile
                string ContentToBeTranslated = await new StreamReader(InputStream).ReadToEndAsync();
               
                //Translate
                switch (FileExtension)
                {
                    case ("html"):
                        TranslatedContent = HTMLTranslationManager.DoContentTranslation(ContentToBeTranslated, FromLang, ToLang);
                        break;
                    case ("htm"):
                        TranslatedContent = HTMLTranslationManager.DoContentTranslation(ContentToBeTranslated, FromLang, ToLang);
                        break;
                    case "txt":
                        TranslatedContent = DocumentTranslationManager.ProcessTextDocument(ContentToBeTranslated,FromLang,ToLang);
                        break;
                    default:
                        break;
                }
                
                
            }
            catch (Exception e)
            {
                log.LogCritical("Exception: " + e.Message);
            }
            finally 
            {
                //Save to Blob
                await OutputText.WriteAsync(TranslatedContent);
                OutputText.Close();

                
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                log.LogInformation($"FINISHED: AutoTranslateBlob function for blob:{name} \n ExecutionTime: {Convert.ToString(elapsedMs)} Ms.");

            }

        }

        #region utilities
        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

     
        #endregion
    }
}
