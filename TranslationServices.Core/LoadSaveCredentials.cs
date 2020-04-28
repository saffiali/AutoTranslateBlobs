using System;

namespace TranslationAssistant.TranslationServices.Core
{
    public static class LoadSaveCredentials
    {

        /// <summary>
        /// Loads credentials from settings file.
        /// Doesn't need to be public, because it is called during Initialize();
        /// </summary>
        public static void LoadCredentials()
        {
            TranslationServiceFacade.AzureKey = GetEnvironmentVariable("AzureKey");
            TranslationServiceFacade.CategoryID = GetEnvironmentVariable("CategoryID");
            TranslationServiceFacade.AppId = GetEnvironmentVariable("AppId");
            TranslationServiceFacade.UseAdvancedSettings = Convert.ToBoolean(GetEnvironmentVariable("UseAdvancedSettings"));
            //TranslationServiceFacade.Adv_CategoryId = GetEnvironmentVariable("Adv_CategoryID");
            TranslationServiceFacade.UseAzureGovernment = Convert.ToBoolean(GetEnvironmentVariable("UseAzureGovernment"));
            TranslationServiceFacade.UseCustomEndpoint = Convert.ToBoolean(GetEnvironmentVariable("UseCustomEndpoint"));
            TranslationServiceFacade.CustomEndpointUrl = GetEnvironmentVariable("CustomEndpointUrl");
            //SpeechServiceFacade.SpeechAccountKey = GetEnvironmentVariable("SpeechAccountKey");
        }

        public static void LoadCredentials(string azureKey, string categoryID, string appId = "")
        {
            TranslationServiceFacade.AzureKey = azureKey;
            TranslationServiceFacade.CategoryID = categoryID;
            TranslationServiceFacade.AppId = appId;
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
            //GetEnvironmentVariable("AzureKey") = TranslationServiceFacade.AzureKey;
            //GetEnvironmentVariable("CategoryID = TranslationServiceFacade.CategoryID;
            //GetEnvironmentVariable("AppId = TranslationServiceFacade.AppId;
            //GetEnvironmentVariable("UseAdvancedSettings = TranslationServiceFacade.UseAdvancedSettings;
            //GetEnvironmentVariable("Adv_CategoryID = TranslationServiceFacade.Adv_CategoryId;
            //GetEnvironmentVariable("UseAzureGovernment = TranslationServiceFacade.UseAzureGovernment;
            //GetEnvironmentVariable("UseCustomEndpoint = TranslationServiceFacade.UseCustomEndpoint;
            //GetEnvironmentVariable("CustomEndpointUrl = TranslationServiceFacade.CustomEndpointUrl;
            //GetEnvironmentVariable("SpeechAccountKey = SpeechServiceFacade.SpeechAccountKey;
            //GetEnvironmentVariable("Save();
        }


        public static string GetEnvironmentVariable(string name)
        {
            return name + ": " +
                System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
