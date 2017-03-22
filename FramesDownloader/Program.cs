using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CsQuery;
using System.Text;

namespace FramesDownloader
{
    /// <summary>
    /// Простая структура с полями адреса и содержимого
    /// </summary>
    class WebContent
    {
        public string url;
        public string html;
    }

    class Program
    {
        /// <summary>
        /// Точка входа в программу
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var site = "http://frame.free.nanoquant.ru/";

            try
            {
                // ожидаем выполнение асинхронного метода в синхронном коде
                FrameDownloaderAsync(site).Wait();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Err: {0}\n{1}", ex.Message, ex.InnerException);
            }
            
            Console.ReadKey();
        }
        

        /// <summary>
        /// Асинхронный метод, получающий индексную страницу, адреса ее фреймов и скачивающий их содержимое
        /// </summary>
        /// <param name="site">Адрес сайта</param>
        /// <returns></returns>
        static async Task FrameDownloaderAsync(string site)
        {
            // получаем индексную страницу
            var indexPage = await GetHtml(site);

            // используем парсер CsQuery для работы с ее содержимым
            CQ cq = CQ.Create(indexPage.html);
            
            // коллекция задач для одновременной загрузки страниц
            var downloaders = new List<Task<WebContent>>();

            // в каждом найденном в индексной странице элементе <frame...>
            foreach (IDomObject obj in cq.Find("frame"))
            {
                // читаем поле src="..."
                var pageUrl = site + obj.GetAttribute("src");
                // формируем задачу для скачивания
                var downloader = GetHtml(pageUrl);
                // помещаем в коллекцию
                downloaders.Add(downloader);
            }
            
            // пока коллекция не пуста
            while (downloaders.Count > 0)
            {
                // ожидаем выполнение любой задачи из нее
                var downloader = await Task.WhenAny(downloaders);
                // удаляем задачу из коллекции
                downloaders.Remove(downloader);
                // получаем результат этой задачи и выводим
                var result = downloader.Result;
                Console.WriteLine("url: {0}\nhtml:{1}", result.url, result.html);

                // и используем данные как душа прикажет...
            }
        }

        /// <summary>
        /// Асинхронный метод, позволяющий получить содержимое страницы по URL.
        /// </summary>
        /// <param name="url">URL для загрузки</param>
        /// <returns>Структуру вида URL/содержимое</returns>
        static async Task<WebContent> GetHtml(string url)
        {
            Console.WriteLine("Getting {0}...", url);
            using (var client = new HttpClient())
            {

                var response = await client.GetAsync(url);
                var html = await response.Content.ReadAsStringAsync();

                Console.WriteLine("Getting {0} - OK", url);
                return new WebContent()
                {
                    url = url,
                    html = html
                };

            }
        }
    }
}