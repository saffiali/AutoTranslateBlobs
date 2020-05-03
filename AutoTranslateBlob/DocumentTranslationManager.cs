using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TranslationAssistant.TranslationServices.Core;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using Saffi.Translate;

namespace Saffi.Translate
{
    public class DocumentTranslationManager
    {




        #region Methods
        /// <summary>
        /// Translates a plain text document in UTF8 encoding to the target language.
        /// </summary>
        /// <param name="ContentToBeTranslated">Text content</param>
        /// <param name="sourceLanguage">From language</param>
        /// <param name="targetLanguage">To language</param>
        public static string ProcessTextDocument(string ContentToBeTranslated, string sourceLanguage, string targetLanguage)
        {
            StringBuilder TranslatedContent = new StringBuilder();

            try
            {
                var lstTexts = Regex.Split(ContentToBeTranslated, "\r\n|\r|\n");
                var batches = SplitList(lstTexts, TranslationServiceFacade.Maxelements, TranslationServiceFacade.Maxrequestsize);

                foreach (var batch in batches)
                {
                    string[] translated = TranslationServiceFacade.TranslateArray(batch.ToArray(), sourceLanguage, targetLanguage);
                    TranslatedContent.Append(StringArrayToString(translated));
                }

                return TranslatedContent.ToString();
            }
            catch (Exception e)
            {

                return TranslatedContent.ToString();
            }
            
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Splits the list.
        /// </summary>
        /// <param name="values">
        ///  The values to be split.
        /// </param>
        /// <param name="groupSize">
        ///  The group size.
        /// </param>
        /// <param name="maxSize">
        ///  The max size.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///  The System.Collections.Generic.List`1[T -&gt; System.Collections.Generic.List`1[T -&gt; T]].
        /// </returns>
        private static List<List<T>> SplitList<T>(IEnumerable<T> values, int groupSize, int maxSize)
        {
            List<List<T>> result = new List<List<T>>();
            List<T> valueList = values.ToList();
            int startIndex = 0;
            int count = valueList.Count;

            while (startIndex < count)
            {
                int elementCount = (startIndex + groupSize > count) ? count - startIndex : groupSize;
                while (true)
                {
                    var aggregatedSize =
                        valueList.GetRange(startIndex, elementCount)
                            .Aggregate(
                                new StringBuilder(),
                                (s, i) => s.Length < maxSize ? s.Append(i) : s,
                                s => s.ToString())
                            .Length;
                    if (aggregatedSize >= maxSize)
                    {
                        if (elementCount == 1) break;
                        elementCount = elementCount - 1;
                    }
                    else
                    {
                        break;
                    }
                }

                result.Add(valueList.GetRange(startIndex, elementCount));
                startIndex += elementCount;
            }

            return result;
        }

        /// <summary>
        /// Covert Array to String
        /// </summary>
        /// <param name="array">Array of Strings</param>
        /// <returns>Joined String</returns>
        static string StringArrayToString(string[] array)
        {
            // Concatenate all the elements into a StringBuilder.
            StringBuilder builder = new StringBuilder();
            foreach (string value in array)
            {
                builder.Append(value);
                builder.Append('.');
            }
            return builder.ToString();
        }
        #endregion


    }
}
