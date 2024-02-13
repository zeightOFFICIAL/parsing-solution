using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;


namespace ParsingSolution
{
    internal class ProductParser
    {
        private const string fileNameToWrite = "result.csv";
        private readonly string productUrlToParse;
        private static IConfiguration activeConfigFromRoot;
        private static IBrowsingContext activeContextFromRoot;
        private List<string> values;

        /// <summary>
        /// Transforms list of strings to combined string. Changes nothing. No async.
        /// </summary>
        /// <param name="listToTransform">List to transform</param>
        /// <returns>String of transformed list.</returns>
        private static string TransformListToString(List<string> listToTransform)
        {
            var strTransformed = "";

            foreach (var valueFromList in listToTransform) {
                strTransformed = strTransformed + valueFromList + ",";
            }

            // simple workaround to remove last ,
            return strTransformed.Remove(strTransformed.Length - 1);
        }

        /// <summary>
        /// Combines elements of the list of breadcrumb strings to combined string. 
        /// Changes nothing. No async.
        /// </summary>
        /// <param name="listBreadcrumbs">List of breadcrumbs to transform</param>
        /// <returns>String of combined crumbs.</returns>
        private static string CombineBreadcrumbsListToString(List<string> listBreadcrumbs)
        {
            var combinedBreadcrumbs = "";

            foreach (var crumbStep in  listBreadcrumbs) {
                combinedBreadcrumbs = combinedBreadcrumbs + crumbStep + "/";
            }

            return combinedBreadcrumbs;
        }

        /// <summary>
        /// Combines elements of the list of value strings to combined csv row string. 
        /// Changes nothing. No async.
        /// </summary>
        /// <param name="valuesList">List of values</param>
        /// <returns>String - csv row of values with ; at the end.</returns>
        private static string ValuesListToCsv(List<string> valuesList)
        {
            string csvRow = "";

            foreach (var value in valuesList) {
                csvRow += value + ";";
            }

            return csvRow;
        }

        /// <summary>
        /// Writes line of data to the file asyncly.
        /// Changes nothing. Async. Uses FileStream, StreamWriter. Returns nothing.
        /// </summary>
        /// <param name="data">Data to write.</param>
        /// <returns></returns>
        private async Task WriteToFileAsync(string data)
        {
            using (FileStream fileStream = new FileStream(fileNameToWrite, FileMode.Append, FileAccess.Write, FileShare.None, 4096, true))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            {
                await streamWriter.WriteLineAsync(data);
            }
        }

        /// <summary>
        /// ProductParser constructor. Creates an instance of ProductParser.
        /// </summary>
        /// <param name="rootConfig">Root's config to inherit</param>
        /// <param name="rootContext">Root's context to inherit</param>
        /// <param name="url">URL to parse.</param>
        /// <returns></returns>
        public ProductParser(IConfiguration rootConfig, IBrowsingContext rootContext, string url)
        {
            activeConfigFromRoot = rootConfig;
            activeContextFromRoot = rootContext;
            productUrlToParse = url;
        }

        /// <summary>
        /// Parsing function to navigate through document at URL, indicated in constructor, to find needed values.
        /// Utilizes LINQ. Async task. Return nothing. Briefly stores values in List.
        /// </summary>
        /// <returns></returns>
        public async Task ParseDocument() { 
            var documentPlain = await activeContextFromRoot.OpenAsync(productUrlToParse);
            string regionStr = "nan", costStr = "nan", nameStr = "nan";
            List<string> crumbsList, imgUrlList_full, imgUrlList_resize;

            // check whether the node is reachable/accesible
            if (documentPlain.QuerySelector(".detail__item-price-new > .detail__item-price-fact") == null ||
                documentPlain.QuerySelector(".city-select__select > option[selected]") == null ||
                documentPlain.QuerySelectorAll(".shops__breadcrumbs-items > .shops__breadcrumbs-item a > span, .shops__breadcrumbs-items > .shops__breadcrumbs-item span > span") == null ||
                documentPlain.QuerySelector(".title-line > h1") == null ||
                documentPlain.QuerySelectorAll(".constructor-popup__subgall-item > img") == null ||
                documentPlain.QuerySelectorAll("img[alt=\"Цепь\"], img[alt=\"Упаковка\"]") == null) {
                return;
            } 

            // parse nodes content
            regionStr = documentPlain.QuerySelector(".city-select__select > option[selected]").TextContent;
            crumbsList = documentPlain.QuerySelectorAll(".shops__breadcrumbs-items > .shops__breadcrumbs-item a > span, .shops__breadcrumbs-items > .shops__breadcrumbs-item span > span")
                .Select(content => content.TextContent.Trim())
                .ToList();
            nameStr = documentPlain.QuerySelector(".title-line > h1").TextContent.TrimEnd();
            costStr = documentPlain.QuerySelector(".detail__item-price-new > .detail__item-price-fact").TextContent.Trim().Replace(" ", "");
            imgUrlList_full = documentPlain.QuerySelectorAll(".constructor-popup__subgall-item > img")
                .Select(src => src.GetAttribute("data-full"))
                .ToList();
            imgUrlList_resize = documentPlain.QuerySelectorAll("img[alt=\"Цепь\"], img[alt=\"Упаковка\"]")
                .Select(src => src.GetAttribute("src"))
                .ToList();

            string crumbsString = CombineBreadcrumbsListToString(crumbsList).TrimEnd();
            string imgUrlString_full = TransformListToString(imgUrlList_full);
            string imgUrlString_resize = TransformListToString(imgUrlList_resize);

            // brief storage of values, are to be deleted at the await in Main by garbage collector
            values = new List<string> { regionStr, crumbsString, nameStr, costStr, imgUrlString_full, imgUrlString_resize };
            await WriteToFileAsync(ValuesListToCsv(values));
        }
        
    }
}
