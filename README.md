# AutoTranslateBlobs
This is an Azure Function to Auto Translate HTML files once the arrive in a Blob container. Then store the translated one in a different container

# Challenges
- Maintain the structure of HTML document.
- Address the API Limit of 5000 charcter per call, so we need to slice the html content.

# Requirments
You need to provide the following configuration or app settings either from the portal or by creating "local.settings.json" to test locally.
```
    "AzureWebJobsStorage": "",
    "FromLang": "English",
    "ToLang": "Arabic",
    "AzureTranslateKey": "c4c50e8f54af452cbfbf3d8e227f68d8",
```
# Credits
I ported the "TranslationAssistant.TranslationServices.Core" from "DocumentTranslator" to dotnet standard to be able to use it in Functions runtime.

DocumentTranslator: https://github.com/MicrosoftTranslator/DocumentTranslator

# Porting to .Net standard 3.0
https://devblogs.microsoft.com/dotnet/how-to-port-desktop-applications-to-net-core-3-0/
https://devblogs.microsoft.com/dotnet/are-your-windows-forms-and-wpf-applications-ready-for-net-core-3-0/
