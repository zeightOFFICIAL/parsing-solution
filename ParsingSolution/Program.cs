using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;

namespace ParsingSolution
{
    internal class Program
    {
        static IConfiguration rootConfig;
        static IBrowsingContext rootContext;
        static readonly string rootUrl = "https://miuz.ru/";
        static readonly string mainUrl = string.Concat(rootUrl, "catalog/chain/");
        const string pageIncrementStr = "page=";
        static uint lastPageIndex = 1;

        /// <summary>
        /// Finds all links to all products on the single page. Changes nothing. Utilizes LINQ.
        /// No async. Return List of strings.
        /// </summary>
        /// <param name="pageDocument">Document instance for page</param>
        /// <returns>List of all the founded products on the page</returns>
        static List<string> FindProductPagesURL(IDocument pageDocument)
        {
            List<string> linksList = pageDocument.QuerySelectorAll(".product > a")
                .Select(link => link.GetAttribute("href"))
                .ToList();
            return linksList;
        }

        static void Main(string[] args)
        {
            // quick fix for console output, can be removed afterwards
            Console.OutputEncoding = Encoding.UTF8;
            // method to parse root page, meaning https://miuz.ru/catalog/chain/
            rootParse();
            // quick fix for console output.
            Console.ReadLine();
        }

        /// <summary>
        /// Root function for async parsing. Initiates parsing at zero (root) url and starts parsing
        /// further. Finds last page index, inits a for iteration to parse all the pages accordingly. 
        /// Parsing task organized as follows: one page initiates async parsing function for each
        /// product. Next page awaits the previous one so to counter-meassure the max-depth of async.
        /// Returns nothing. Changes rootConfig, rootContext. Async.
        /// </summary>
        /// <returns></returns>
        static async void rootParse()
        {
            // set up browser session
            CookieContainer cks = new CookieContainer();
            cks.Add(new Url("https://miuz.ru"), new Cookie("BITRIX_SM_CityId", "4500"));
            cks.Add(new Url("https://miuz.ru"), new Cookie("BITRIX_SM_City", "%D0%A0%D0%BE%D1%81%D1%82%D0%BE%D0%B2-%D0%BD%D0%B0-%D0%94%D0%BE%D0%BD%D1%83"));
            rootConfig = Configuration.Default.WithDefaultLoader().With(cks);
            rootContext = BrowsingContext.New(rootConfig);

            var rootPage = await rootContext.OpenAsync(mainUrl);

            lastPageIndex = Convert.ToUInt16(rootPage.QuerySelectorAll(".b-pagination__link").Last().TextContent);
            string nextPageAddendum = "";
            
            for (uint eachPage = 1; eachPage <= lastPageIndex; eachPage++)
            {
                await pageParse(mainUrl + nextPageAddendum);
                nextPageAddendum = pageIncrementStr + eachPage.ToString() + "/";
            }
        }

        /// <summary>
        /// Parses specific page for all the products within. Finds URL for each product. Initiate 
        /// async for each product parser. 
        /// Returns nothing. Changes nothing. Async.
        /// </summary>
        /// <param name="pageUrl">Page url to parse</param>
        /// <returns></returns>
        static async Task pageParse(string pageUrl)
        {
            var pageDocument = await rootContext.OpenAsync(pageUrl);

            var productPagesURL = FindProductPagesURL(pageDocument);

            foreach (var productURL in productPagesURL)
            {
                var productPageURLTrimmed = productURL.Remove(0,1);
                var parser = new ProductParser(rootConfig, rootContext, rootUrl + productPageURLTrimmed);
                await parser.ParseDocument();
            }
        }
    }
}
