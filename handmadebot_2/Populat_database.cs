using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using static handmadebot_2.Program;

namespace handmadebot_2
{
    public static class Populat_database
    {
        public /*async*/ static void populate(){
            while (true) 
            {
                try 
                {
                    Story_main main = new Story_main();
                    /*await*/ main.Populate().Wait();
                    foreach (Story_main.Data i in main.Story)
                    {
                        using (var connection = new SQLiteConnection("Data Source=database.sqlite3"))
                        {
                            /*await*/ connection.Open/*Async*/();
                            string check = string.Format("SELECT ID " +
                                                           "FROM Data_news " +
                                                          "WHERE Title_list_new = @Title_list_new " +
                                                          "AND Picture_list_news = @Picture_list_news");
                            SQLiteCommand check_command = new SQLiteCommand(check, connection);
                            check_command.Parameters.AddWithValue("Title_list_new", i.Title_list_new);
                            check_command.Parameters.AddWithValue("Picture_list_news", i.Picture_list_news);
                            using (var reader = /*await*/ check_command.ExecuteReader/*Async*/())
                            {
                                /*await*/ reader.Read/*Async*/();
                                if (!reader.HasRows)
                                {
                                    string query = string.Format("INSERT INTO Data_news" + "(Title_list_new, " +
                                                                                          "Type_list_news, " +
                                                                                          "Href_yandex_news, " +
                                                                                          "Picture_list_news, " +
                                                                                          "Text_yandex_news, " +
                                                                                          "Href_sourse_news, " +
                                                                                          "Agency_yandex_news, " +
                                                                                          "Date_create) " +
                                                                 "VALUES(@Title_list_new, " +
                                                                        "@Type_list_news, " +
                                                                        "@Href_yandex_news, " +
                                                                        "@Picture_list_news, " +
                                                                        "@Text_yandex_news, " +
                                                                        "@Href_sourse_news, " +
                                                                        "@Agency_yandex_news, " +
                                                                        "datetime());");

                                    SQLiteCommand comand = new SQLiteCommand(query, connection);
                                    comand.Parameters.AddWithValue("Title_list_new", i.Title_list_new);
                                    comand.Parameters.AddWithValue("Type_list_news", i.Type_list_news);
                                    comand.Parameters.AddWithValue("Href_yandex_news", i.Href_yandex_news);
                                    comand.Parameters.AddWithValue("Picture_list_news", i.Picture_list_news);
                                    comand.Parameters.AddWithValue("Text_yandex_news", i.Text_yandex_news);
                                    comand.Parameters.AddWithValue("Href_sourse_news", i.Href_sourse_news);
                                    comand.Parameters.AddWithValue("Agency_yandex_news", i.Agency_yandex_news);


                                    /*await*/ comand.ExecuteNonQuery/*Async*/();
                                }
                                /*await*/ reader.Close/*Async*/();
                            }
                            connection.Close();
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Произошел сбой в работе функции populate при заполнении БД главной новостью");
                    Console.WriteLine($"Исключение: {ex.Message}");
                }

                /*await*/ SendNews().Wait();
                Console.WriteLine("Один цикл обновления БД отработал");
                //Thread.Sleep(180000);
                /*await*/ Task.Delay(14400000).Wait();
            }
        }
        public async static Task SendNews(){
            using (var connection = new SQLiteConnection("Data Source=database.sqlite3")){
                await connection.OpenAsync();
                string query = "SELECT id, " +
                                      "Title_list_new, " +
                                      "Type_list_news, " +
                                      "Picture_list_news, " +
                                      "Text_yandex_news, " +
                                      "Href_sourse_news, " +
                                      "Agency_yandex_news, " +
                                      "Text_sourse_news " +
                               "FROM Data_news " +
                               "WHERE Status is null";
                SQLiteCommand command = new SQLiteCommand(query, connection);
                var reader = await command.ExecuteReaderAsync();
                while(await reader.ReadAsync()) 
                {
                    string id = reader["id"].ToString();
                    string title = reader["Title_list_new"].ToString();
                    string type = reader["Type_list_news"].ToString();
                    string picture = reader["Picture_list_news"].ToString();
                    string short_text = reader["Text_yandex_news"].ToString();
                    string href = reader["Href_sourse_news"].ToString();
                    string agency = reader["Agency_yandex_news"].ToString();

                    //Console.WriteLine(id);
                    //Console.WriteLine(title);
                    //Console.WriteLine(type);
                    //Console.WriteLine(picture);
                    //Console.WriteLine(short_text);
                    //Console.WriteLine(href);
                    //Console.WriteLine(agency);

                    bool check = (title != "")
                              && (type != "")
                              && (picture != "")
                              && (short_text != "")
                              && (href != "")
                              && (agency != "");
                    if (check)
                    {
                        try
                        {
                            string format_text = "\n" + type + "\n" + title + "\n" + short_text + "\n" + picture + "\n" + agency + "\n" + href + "\n";
                            List<List<InlineKeyboardButton>> bottons = new List<List<InlineKeyboardButton>>();
                            bottons.Add(new List<InlineKeyboardButton>());
                            bottons[0].Add(new InlineKeyboardButton { Text = "Стандарт. публик. 👍/👎", CallbackData = "standart_public_bot" });
                            bottons.Add(new List<InlineKeyboardButton>());
                            bottons[1].Add(new InlineKeyboardButton { Text = "Удалить ❌", CallbackData = "delete_message_bot" });

                            InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(bottons);

                            await botClient.SendTextMessageAsync(text: format_text,
                                                                 chatId: 896172479,
                                                                 parseMode: ParseMode.Html,
                                                                 disableNotification: true,
                                                                 disableWebPagePreview: true,
                                                                 replyMarkup: keyboard);

                            string update_rec = string.Format("UPDATE Data_news SET Status = 'Y' WHERE id = @id");
                            SQLiteCommand comand = new SQLiteCommand(update_rec, connection);
                            comand.Parameters.AddWithValue("id", id);
                            await comand.ExecuteNonQueryAsync();
                        }
                        catch(Exception ex) 
                        {
                            Console.WriteLine("Произошел сбой во время отправки новости из БД, метод Populat_database.SendNews");
                            Console.WriteLine($"Исключение: {ex.Message}");
                        }
                    }
                }
                await reader.CloseAsync();
                await connection.CloseAsync();
            }
        }
    }
}
