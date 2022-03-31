using System;
using System.Collections.Generic;
using System.Text;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Dom;
using System.Net;
using AngleSharp.Io;
using System.Threading;
using AngleSharp.Html.Dom;
using System.Threading.Tasks;

namespace handmadebot_2
{
    public class Story_main
    {

        async public Task Populate() {
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var parser = context.GetService<IHtmlParser>();
            IHtmlDocument document;
            try
            {
                using (WebClient client = new WebClient()){
                    client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 80.0.3987.163 Safari / 537.36");
                    string htmlCode = await client.DownloadStringTaskAsync("https://yandex.ru/news/region/tyumen");
                    document = await parser.ParseDocumentAsync(htmlCode);
                    client.CancelAsync();
                }

                //заголовки новости
                var documentTitleList = document.QuerySelectorAll("#neo-page article.mg-card.news-card.news-card_type_image.mg-grid__item.mg-grid__item_type_card h2.news-card__title");
                //ссылка на новость Яндекс
                var documentHrefYandexNews = document.QuerySelectorAll("#neo-page article.mg-card.news-card.news-card_type_image.mg-grid__item.mg-grid__item_type_card a.news-card__link");
                
                if (documentTitleList.Length != documentHrefYandexNews.Length)
                {
                    throw new Exception("Ошибка во время парсинга страницы, число заголовков не совпадает с числом ссылок на новость!");
                }

                //Запихиваем теперь эти части в коллекцию.
                for (int i = 0; i < documentTitleList.Length; i++)
                {
                    Story.Add(new Data() { Title_list_new = documentTitleList[i].Text() });
                    //я хер его знает иногда ссылки пачимута без https://yandex.ru
                    Story[i].Href_yandex_news = documentHrefYandexNews[i].GetAttribute("href").Contains("https://yandex.ru") == true ? documentHrefYandexNews[i].GetAttribute("href") : "https://yandex.ru" + documentHrefYandexNews[i].GetAttribute("href");
                }

                for(int i = 0; i < Story.Count; i++)
                {
                    using (WebClient client = new WebClient())
                    {
                        string htmlCode = await client.DownloadStringTaskAsync(Story[i].Href_yandex_news);
                        document = await parser.ParseDocumentAsync(htmlCode);
                        client.CancelAsync();
                    }

                    //класс новости
                    foreach (IElement elem in document.QuerySelectorAll("div.mg-navigation-menu__items div.mg-navigation-menu__item-wrap-inner.mg-navigation-menu__item-wrap-inner_active"))
                    {
                        Story[i].Type_list_news = elem.Text();
                    }

                    //картинка к новости
                    foreach (IElement elem in document.QuerySelectorAll("div.news-media-stack__picture-viewer img.news-media-stack__picture-viewer-img"))
                    {
                        Story[i].Picture_list_news = elem.GetAttribute("src");
                    }

                    //текст новости
                    foreach (IElement elem in document.QuerySelectorAll("div.news-story__content span.news-story__text"))
                    {
                        Story[i].Text_yandex_news = elem.Text();
                    }

                    //бывает что они переключают верстку
                    if (Story[i].Text_yandex_news == null)
                    {
                        foreach (IElement elem in document.QuerySelectorAll("div.mg-story__content span.mg-story__text"))
                        {
                            Story[i].Text_yandex_news = elem.Text();
                        }
                    }

                    //имя издателя
                    foreach (IElement elem in document.QuerySelectorAll("a.news-story__subtitle span.news-story__subtitle-text"))
                    {
                        Story[i].Agency_yandex_news = elem.Text();
                    }

                    //ссылка на саму новость
                    foreach (IElement elem in document.QuerySelectorAll("a.news-story__subtitle"))
                    {
                        Story[i].Href_sourse_news = elem.GetAttribute("href");
                    }
                }

            }
            catch(Exception ex){
                Console.WriteLine("Произошел сбой в работе функции Populate класса Story_main");
                Console.WriteLine($"Исключение: {ex.Message}");
            }

        }

        public class Data
        {
            public string Title_list_new { get; set; } = null; //Заголовок новости
            public string Type_list_news { get; set; } = null; //Категория новости
            public string Href_yandex_news { get; set; } = null; //Ссылка новости Яндекс
            public string Picture_list_news { get; set; } = null; //Картинка к заголовку новости Яндекс
            public string Text_yandex_news { get; set; } = null; //Текст новости Яндекса
            public string Href_sourse_news { get; set; } = null; //Ссылка на саму новость
            public string Agency_yandex_news { get; set; } = null; //Имя издателя
            public string Text_sourse_news { get; set; } = null; //Основной текст новости
        };
        public List<Data> Story = new List<Data>();
    }
}
