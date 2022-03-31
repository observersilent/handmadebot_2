using System;
using System.Threading;
using System.Data.SQLite;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InlineQueryResults;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Collections;
using System.Collections.Generic;
using Telegram.Bot.Types.Enums;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;
using System.Linq;

namespace handmadebot_2
{
    class Program
    {
        public static TelegramBotClient botClient;
        static void Main(string[] args)
        {
            botClient = new TelegramBotClient(SecretKey.API_KEY) { Timeout = TimeSpan.FromSeconds(10) };
            Create_Database.Create();

            var me = botClient.GetMeAsync().Result;
            Console.WriteLine($"I am user {me.Id} and my name is {me.FirstName}.");

            //botClient.OnInlineQuery += Method;
            botClient.OnCallbackQuery += CallbackQuery;
            botClient.OnMessage += Message;
            botClient.StartReceiving();

            Task task = new Task(() => Populat_database.populate());
            task.Start();
            task.Wait();



            //List<List<InlineKeyboardButton>> bottons = new List<List<InlineKeyboardButton>>();
            //bottons.Add(new List<InlineKeyboardButton>());
            //bottons[0].Add(new InlineKeyboardButton { Text = "👍", CallbackData = "like_message_bot" });
            //bottons[0].Add(new InlineKeyboardButton { Text = "👎", CallbackData = "dislike_message_bot" });
            //InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(bottons);

            //botClient.EditMessageReplyMarkupAsync(-1001466398761, 1355, keyboard);

            //Console.ReadKey();
        }

        public static async void CallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            if (e.CallbackQuery.Data == "delete_message_bot")
            {
                try
                {
                    long chat_id = e.CallbackQuery.Message.Chat.Id;
                    int message_id = e.CallbackQuery.Message.MessageId;
                    await botClient.AnswerCallbackQueryAsync(callbackQueryId: e.CallbackQuery.Id, text: "Удаляю", showAlert: false);
                    await botClient.DeleteMessageAsync(chatId: chat_id, messageId: message_id);
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(text: $"Ошибка: {ex.Message}",
                       chatId: e.CallbackQuery.From.Id,
                       parseMode: ParseMode.Html,
                       disableNotification: true,
                       disableWebPagePreview: false); ;
                }

            }

            if (e.CallbackQuery.Data == "standart_public_bot")
            {
                string[] rows = e.CallbackQuery.Message.Text.Split(new char[] { '\n' });

                string format_text = getFormatTextNews(rows, null);
                InlineKeyboardMarkup keyboard = getKeyboard(null);
               
                await botClient.SendTextMessageAsync(text: format_text,
                                               chatId: SecretKey.CHANNEL,
                                               parseMode: ParseMode.Html,
                                               disableNotification: true,
                                               disableWebPagePreview: false,
                                               replyMarkup: keyboard);

                await botClient.AnswerCallbackQueryAsync(callbackQueryId: e.CallbackQuery.Id, text: "Публикую", showAlert: false);
                await botClient.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);

            }

            if (e.CallbackQuery.Data == "like_message_bot" ||
                e.CallbackQuery.Data == "dislike_message_bot" ||
                e.CallbackQuery.Data == "feedback_1_message_bot" ||
                e.CallbackQuery.Data == "feedback_2_message_bot" ||
                e.CallbackQuery.Data == "feedback_3_message_bot")
            {
                calculateChangeBd(e);
            }

        }

        //изменяем отображение значений на кнопках сообщения в соответствии с состоянием БД
        public static async void calculateChangeMsg(CallbackQueryEventArgs e)
        {
            long chat_id = SecretKey.CHANNEL; ;
            int message_id;

            //подменяем message_id сообщения на id сообщения в канале (это позволит менять сообщения из чата)
            if (e.CallbackQuery.Message.Chat.Id == chat_id)
            {
                message_id = e.CallbackQuery.Message.MessageId;
            }
            else
            {
                message_id = e.CallbackQuery.Message.ForwardFromMessageId;
            }

            //получаем названия кнопок
            List<List<string>> listButton = new List<List<string>>();

            var ReplayMarkupEN = e.CallbackQuery.Message.ReplyMarkup.InlineKeyboard.GetEnumerator();

            for (int i = 0; ReplayMarkupEN.MoveNext(); i++)
            {
                listButton.Add(new List<string>());
                var InlineKeyboardEN = ReplayMarkupEN.Current.GetEnumerator();
                for (int j = 0; InlineKeyboardEN.MoveNext(); j++)
                {
                    listButton[i].Add(InlineKeyboardEN.Current.CallbackData);
                }
            }

            //получаем текст на кнопках
            List<List<string>> listText = new List<List<string>>();

            for (int i = 0; i < listButton.Count; i++)
            {
                listText.Add(new List<string>());
                for (int j = 0; j < listButton[i].Count; j++)
                {
                    listText[i].Add(await getTextFeedback(chat_id, message_id, listButton[i][j]));
                }
            }

            //получаем цифровые значения на кнопках
            List<List<int>> listValue = new List<List<int>>();

            for (int i = 0; i < listButton.Count; i++)
            {
                listValue.Add(new List<int>());
                for (int j = 0; j < listButton[i].Count; j++)
                {
                    listValue[i].Add(await getCountFeedback(chat_id, message_id, listButton[i][j]));
                }
            }


            List<List<InlineKeyboardButton>> bottons = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < listButton.Count; i++)
            {
                bottons.Add(new List<InlineKeyboardButton>());
                for (int j = 0; j < listButton[i].Count; j++)
                {
                    var textOnButton = listValue[i][j] == 0 ? listText[i][j] : listText[i][j] + " " + listValue[i][j];
                    bottons[i].Add(new InlineKeyboardButton { Text = textOnButton, CallbackData = listButton[i][j] });
                }
            }

            InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(bottons);

            await botClient.EditMessageReplyMarkupAsync(chat_id, message_id, keyboard);

        }

        //функция изменяет состояние бд и отображение постов в соответствии с пришедшим колбеком, 
        public static async void calculateChangeBd(CallbackQueryEventArgs e)
        {
            long chat_id;
            long message_id;
            long from_id;
            string type;
            string old_type;
            var textButton = e.CallbackQuery.Message.ReplyMarkup.InlineKeyboard.ToList().ConvertAll(x => x.ToList());


            chat_id = SecretKey.CHANNEL;
            from_id = e.CallbackQuery.From.Id;
            type = e.CallbackQuery.Data;
            if (e.CallbackQuery.Message.Chat.Id == chat_id)
            {
                message_id = e.CallbackQuery.Message.MessageId;
            }
            else
            {
                message_id = e.CallbackQuery.Message.ForwardFromMessageId;
            }

            old_type = await getUserPreviousFeedback(chat_id, message_id, from_id);

            if (old_type != null && old_type != type) //замена голоса
            {
                updateWhoFeadback(chat_id, message_id, from_id, type);
                updateСountFeedback(chat_id, message_id, old_type, "down", textButton);
                updateСountFeedback(chat_id, message_id, type, "up", textButton);
                calculateChangeMsg(e);
                await botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id, showAlert: false, text: "Ваш голос учтен!");
            }
            if (old_type == null) //новый голос
            {
                updateWhoFeadback(chat_id, message_id, from_id, type);
                updateСountFeedback(chat_id, message_id, type, "up", textButton);
                calculateChangeMsg(e);
                await botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id, showAlert: false, text: "Ваш голос учтен!");
            }
            if (old_type == type) //повторное нажатие
            {
                await botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id, showAlert: false, text: "Вы уже проголосовали");
            }

        }

        //обновление таблицы с данными о том кто оставил колбэк к каждой записи
        public static async void updateWhoFeadback(long chat_id, long message_id, long from_id, string type)
        {
            using (var connection = new SQLiteConnection("Data Source=database.sqlite3"))
            {
                await connection.OpenAsync();
                string check = string.Format("SELECT ID, Type " +
                                               "FROM Who_feedback " +
                                              "WHERE Chat_id = @Chat_id " +
                                                "AND Message_id = @Message_id " +
                                                "AND From_id = @From_id");

                SQLiteCommand check_command = new SQLiteCommand(check, connection);
                check_command.Parameters.AddWithValue("Chat_id", chat_id);
                check_command.Parameters.AddWithValue("Message_id", message_id);
                check_command.Parameters.AddWithValue("From_id", from_id);

                using (var reader = await check_command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    if (!reader.HasRows)
                    {
                        string query = string.Format("INSERT INTO Who_feedback" + "(Chat_id, " +
                                                                                   "Message_id, " +
                                                                                   "From_id, " +
                                                                                   "Type, " +
                                                                                   "Date_create) " +
                                                          "VALUES(@Chat_id, " +
                                                                 "@Message_id, " +
                                                                 "@From_id, " +
                                                                 "@Type, " +
                                                                 "datetime());");

                        SQLiteCommand comand = new SQLiteCommand(query, connection);
                        comand.Parameters.AddWithValue("Chat_id", chat_id);
                        comand.Parameters.AddWithValue("Message_id", message_id);
                        comand.Parameters.AddWithValue("From_id", from_id);
                        comand.Parameters.AddWithValue("Type", type);
                        await comand.ExecuteNonQueryAsync();

                        query = string.Format("INSERT INTO Сount_feedback" + "(Chat_id, " +
                                                                             "Message_id, " +
                                                                             "Count_like, " +
                                                                             "Count_dislike, " +
                                                                             "Count_feedback_1, " +
                                                                             "Count_feedback_2, " +
                                                                             "Count_feedback_3) " +
                                                   "VALUES(@Chat_id, " +
                                                   "@Message_id, " +
                                                   "@Count_like, " +
                                                   "@Count_dislike, " +
                                                   "@Count_feedback_1, " +
                                                   "@Count_feedback_2, " +
                                                   "@Count_feedback_3);");
                    }
                    else
                    {
                        string query = string.Format("UPDATE Who_feedback SET Type = @Type WHERE id = @id");
                        SQLiteCommand comand = new SQLiteCommand(query, connection);
                        comand.Parameters.AddWithValue("Type", type);
                        comand.Parameters.AddWithValue("id", reader["id"].ToString());
                        await comand.ExecuteNonQueryAsync();
                    }
                    await reader.CloseAsync();
                }
                connection.Close();
            }
        }

        //обновление таблицы с количеством колбэков по типу
        public static async void updateСountFeedback(long chat_id, long message_id, string type, string operation, List<List<InlineKeyboardButton>> textButton)
        {
            int Count_like;
            int Count_dislike;
            int Count_feedback_1;
            int Count_feedback_2;
            int Count_feedback_3;
            string Text_like = null;
            string Text_dislike = null;
            string Text_feedback_1 = null;
            string Text_feedback_2 = null;
            string Text_feedback_3 = null;
            int id = 0;

            for (int i = 0; i < textButton.Count; i++)
            {
                for (int j = 0; j < textButton[i].Count; j++)
                {
                    switch (textButton[i][j].CallbackData)
                    {
                        case "like_message_bot":
                            Text_like = textButton[i][j].Text;
                            break;
                        case "dislike_message_bot":
                            Text_dislike = textButton[i][j].Text;
                            break;
                        case "feedback_1_message_bot":
                            Text_feedback_1 = textButton[i][j].Text;
                            break;
                        case "feedback_2_message_bot":
                            Text_feedback_2 = textButton[i][j].Text;
                            break;
                        case "feedback_3_message_bot":
                            Text_feedback_3 = textButton[i][j].Text;
                            break;
                    }
                }
            }

            using (var connection = new SQLiteConnection("Data Source=database.sqlite3"))
            {
                await connection.OpenAsync();
                string check = string.Format("SELECT ID, Count_like, Count_dislike, Count_feedback_1, Count_feedback_2, Count_feedback_3 " +
                                               "FROM Сount_feedback " +
                                              "WHERE Chat_id = @Chat_id " +
                                                "AND Message_id = @Message_id");

                SQLiteCommand check_command = new SQLiteCommand(check, connection);
                check_command.Parameters.AddWithValue("Chat_id", chat_id);
                check_command.Parameters.AddWithValue("Message_id", message_id);

                using (var reader = await check_command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();

                    if (!reader.HasRows)
                    {
                        Count_like = 0;
                        Count_dislike = 0;
                        Count_feedback_1 = 0;
                        Count_feedback_2 = 0;
                        Count_feedback_3 = 0;

                        switch (type)
                        {
                            case "like_message_bot":
                                Count_like = 1;
                                break;
                            case "dislike_message_bot":
                                Count_dislike = 1;
                                break;
                            case "feedback_1_message_bot":
                                Count_feedback_1 = 1;
                                break;
                            case "feedback_2_message_bot":
                                Count_feedback_2 = 1;
                                break;
                            case "feedback_3_message_bot":
                                Count_feedback_3 = 1;
                                break;
                        }

                        string query = string.Format("INSERT INTO Сount_feedback" + "(Chat_id, " +
                                                                                   "Message_id, " +
                                                                                   "Count_like, " +
                                                                                   "Count_dislike, " +
                                                                                   "Count_feedback_1, " +
                                                                                   "Count_feedback_2, " +
                                                                                   "Count_feedback_3, " +
                                                                                   "Text_like, " +
                                                                                   "Text_dislike, " +
                                                                                   "Text_feedback_1, " +
                                                                                   "Text_feedback_2, " +
                                                                                   "Text_feedback_3) " +
                                                          "VALUES(@Chat_id, " +
                                                                 "@Message_id, " +
                                                                 "@Count_like, " +
                                                                 "@Count_dislike, " +
                                                                 "@Count_feedback_1, " +
                                                                 "@Count_feedback_2, " +
                                                                 "@Count_feedback_3, " +
                                                                 "@Text_like, " +
                                                                 "@Text_dislike, " +
                                                                 "@Text_feedback_1, " +
                                                                 "@Text_feedback_2, " +
                                                                 "@Text_feedback_3);");

                        SQLiteCommand comand = new SQLiteCommand(query, connection);
                        comand.Parameters.AddWithValue("Chat_id", chat_id);
                        comand.Parameters.AddWithValue("Message_id", message_id);
                        comand.Parameters.AddWithValue("Count_like", Count_like);
                        comand.Parameters.AddWithValue("Count_dislike", Count_dislike);
                        comand.Parameters.AddWithValue("Count_feedback_1", Count_feedback_1);
                        comand.Parameters.AddWithValue("Count_feedback_2", Count_feedback_2);
                        comand.Parameters.AddWithValue("Count_feedback_3", Count_feedback_3);
                        comand.Parameters.AddWithValue("Text_like", Text_like);
                        comand.Parameters.AddWithValue("Text_dislike", Text_dislike);
                        comand.Parameters.AddWithValue("Text_feedback_1", Text_feedback_1);
                        comand.Parameters.AddWithValue("Text_feedback_2", Text_feedback_2);
                        comand.Parameters.AddWithValue("Text_feedback_3", Text_feedback_3);
                        await comand.ExecuteNonQueryAsync();

                    }
                    else
                    {
                        Count_like = Convert.ToInt32(reader["Count_like"]);
                        Count_dislike = Convert.ToInt32(reader["Count_dislike"]);
                        Count_feedback_1 = Convert.ToInt32(reader["Count_feedback_1"]);
                        Count_feedback_2 = Convert.ToInt32(reader["Count_feedback_2"]);
                        Count_feedback_3 = Convert.ToInt32(reader["Count_feedback_3"]);
                        id = Convert.ToInt32(reader["ID"]);
                    }
                    await reader.CloseAsync();
                }
                connection.Close();
            }

            if (operation == "down")
            {
                switch (type)
                {
                    case "like_message_bot":
                        if (Count_like == 0) Count_like = 0;
                        if (Count_like > 0) Count_like -= 1;
                        break;
                    case "dislike_message_bot":
                        if (Count_dislike == 0) Count_dislike = 0;
                        if (Count_dislike > 0) Count_dislike -= 1;
                        break;
                    case "feedback_1_message_bot":
                        if (Count_feedback_1 == 0) Count_feedback_1 = 0;
                        if (Count_feedback_1 > 0) Count_feedback_1 -= 1;
                        break;
                    case "feedback_2_message_bot":
                        if (Count_feedback_2 == 0) Count_feedback_2 = 0;
                        if (Count_feedback_2 > 0) Count_feedback_2 -= 1;
                        break;
                    case "feedback_3_message_bot":
                        if (Count_feedback_3 == 0) Count_feedback_3 = 0;
                        if (Count_feedback_3 > 0) Count_feedback_3 -= 1;
                        break;
                }
                updateTableCountFeedback(Count_like, Count_dislike, Count_feedback_1, Count_feedback_2, Count_feedback_3, id);
            }

            if (operation == "up")
            {
                switch (type)
                {
                    case "like_message_bot":
                        Count_like += 1;
                        break;
                    case "dislike_message_bot":
                        Count_dislike += 1;
                        break;
                    case "feedback_1_message_bot":
                        Count_feedback_1 += 1;
                        break;
                    case "feedback_2_message_bot":
                        Count_feedback_2 += 1;
                        break;
                    case "feedback_3_message_bot":
                        Count_feedback_3 += 1;
                        break;
                }
                updateTableCountFeedback(Count_like, Count_dislike, Count_feedback_1, Count_feedback_2, Count_feedback_3, id);
            }
        }

        //получение варианта который выбрал юзер в прошлый раз
        public static async Task<string> getUserPreviousFeedback(long chat_id, long message_id, long from_id)
        {
            string rezult;
            using (var connection = new SQLiteConnection("Data Source=database.sqlite3"))
            {
                await connection.OpenAsync();
                string check = string.Format("SELECT Type " +
                                               "FROM Who_feedback " +
                                              "WHERE Chat_id = @Chat_id " +
                                                "AND Message_id = @Message_id " +
                                                "AND From_id = @From_id");

                SQLiteCommand check_command = new SQLiteCommand(check, connection);
                check_command.Parameters.AddWithValue("Chat_id", chat_id);
                check_command.Parameters.AddWithValue("Message_id", message_id);
                check_command.Parameters.AddWithValue("From_id", from_id);

                using (var reader = await check_command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    if (!reader.HasRows)
                    {
                        rezult = null;
                        reader.Close();
                        connection.Close();
                    }
                    else
                    {
                        rezult = reader["Type"].ToString();
                        reader.Close();
                        connection.Close();
                    }
                }
            }
            return rezult;
        }

        public static async void updateTableCountFeedback(int Count_like, int Count_dislike, int Count_feedback_1, int Count_feedback_2, int Count_feedback_3, int id)
        {
            using (var connection = new SQLiteConnection("Data Source=database.sqlite3"))
            {
                await connection.OpenAsync();
                string query = string.Format("UPDATE Сount_feedback SET Count_like = @Count_like," +
                                                         " Count_dislike = @Count_dislike," +
                                                         " Count_feedback_1 = @Count_feedback_1," +
                                                         " Count_feedback_2 = @Count_feedback_2," +
                                                         " Count_feedback_3 = @Count_feedback_3 WHERE id = @id");

                SQLiteCommand comand = new SQLiteCommand(query, connection);
                comand.Parameters.AddWithValue("Count_like", Count_like);
                comand.Parameters.AddWithValue("Count_dislike", Count_dislike);
                comand.Parameters.AddWithValue("Count_feedback_1", Count_feedback_1);
                comand.Parameters.AddWithValue("Count_feedback_2", Count_feedback_2);
                comand.Parameters.AddWithValue("Count_feedback_3", Count_feedback_3);
                comand.Parameters.AddWithValue("id", id);
                await comand.ExecuteNonQueryAsync();
                connection.Close();
            }
        }

        //получение количества фидбеков по варианту для поста
        public static async Task<int> getCountFeedback(long chat_id, int message_id, string feedbackVariant)
        {
            int rezult;
            string variantTable = null;
            switch (feedbackVariant)
            {
                case "like_message_bot":
                    variantTable = "Count_like";
                    break;
                case "dislike_message_bot":
                    variantTable = "Count_dislike";
                    break;
                case "feedback_1_message_bot":
                    variantTable = "Count_feedback_1";
                    break;
                case "feedback_2_message_bot":
                    variantTable = "Count_feedback_2";
                    break;
                case "feedback_3_message_bot":
                    variantTable = "Count_feedback_3";
                    break;
            }

            using (var connection = new SQLiteConnection("Data Source=database.sqlite3"))
            {
                await connection.OpenAsync();
                string check = string.Format("SELECT Count_like, Count_dislike, Count_feedback_1, Count_feedback_2, Count_feedback_3 " +
                                               "FROM Сount_feedback " +
                                              "WHERE Chat_id = @Chat_id " +
                                                "AND Message_id = @Message_id");

                SQLiteCommand check_command = new SQLiteCommand(check, connection);
                //check_command.Parameters.AddWithValue("variantTable", variantTable);
                check_command.Parameters.AddWithValue("Chat_id", chat_id);
                check_command.Parameters.AddWithValue("Message_id", message_id);

                using (var reader = await check_command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    if (!reader.HasRows)
                    {
                        rezult = 0;
                    }
                    else
                    {
                        rezult = Convert.ToInt32(reader[variantTable]);
                    }
                    reader.Close();
                }
                connection.Close();
            }
            return rezult;
        }

        //получение текста варианта по ко категории для поста
        public static async Task<string> getTextFeedback(long chat_id, int message_id, string feedbackVariant)
        {
            string rezult;
            string variantTable = null;
            switch (feedbackVariant)
            {
                case "like_message_bot":
                    variantTable = "Text_like";
                    break;
                case "dislike_message_bot":
                    variantTable = "Text_dislike";
                    break;
                case "feedback_1_message_bot":
                    variantTable = "Text_feedback_1";
                    break;
                case "feedback_2_message_bot":
                    variantTable = "Text_feedback_2";
                    break;
                case "feedback_3_message_bot":
                    variantTable = "Text_feedback_3";
                    break;
            }

            using (var connection = new SQLiteConnection("Data Source=database.sqlite3"))
            {
                await connection.OpenAsync();
                string check = string.Format("SELECT Text_like, Text_dislike, Text_feedback_1, Text_feedback_2, Text_feedback_3 " +
                                               "FROM Сount_feedback " +
                                              "WHERE Chat_id = @Chat_id " +
                                                "AND Message_id = @Message_id");

                SQLiteCommand check_command = new SQLiteCommand(check, connection);
                check_command.Parameters.AddWithValue("Chat_id", chat_id);
                check_command.Parameters.AddWithValue("Message_id", message_id);

                using (var reader = await check_command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    if (!reader.HasRows)
                    {
                        rezult = null;
                    }
                    else
                    {
                        rezult = reader[variantTable].ToString();
                    }
                    reader.Close();
                }
                connection.Close();
            }
            return rezult;

        }

        public static async void Message(object sender, MessageEventArgs e)
        {
            //для теста не сдох ли бот
            if (e.Message.Text == "Тест")
            {
                await botClient.SendTextMessageAsync(chatId: 896172479, "Бот работает в штатном режиме...");
            }

            //если мы пересылаем автоматически созданную заготовку новости, тогда мы редактируем новость (добавляем комментарии/кнопки)
            
            if (e?.Message?.ReplyToMessage?.Text != null && e?.Message?.From?.Id == SecretKey.OWNER_ID)
            {
                try
                {
                    if (isCommentNews(e))
                    {
                        //разбираем что указано в комментарии
                        string comment = e?.Message.Text;
                        string[] rows_comment = e?.Message.Text.Split(new char[] { '\n' });
                        List<List<string>> list_comment = new List<List<string>>();
                        for (int i = 0; i < rows_comment.Length; i++)
                        {
                            list_comment.Add(new List<string>());
                            string[] parts_comment = rows_comment[i].Split(new char[] { '/' });

                            for (int j = 0; j < parts_comment.Length; j++)
                            {
                                list_comment[i].Add(parts_comment[j]);
                            }
                        }

                        bool isManualFormat = true; //указывает определено ли форматирование внутри поста или просто указан комментарий
                        string nameComand; //имя команды
                        int coundParams; //количество параметров

                        //проверяем корректно ли заполнены команды комментария
                        for (int i = 0; i < list_comment.Count; i++)
                        {
                            //проверяем правильные ли команды в комментарии 
                            if (!SecretKey.comandComment.Contains(list_comment[i][0].ToUpper()))
                            {
                                isManualFormat = false;
                                break;
                            }

                            //проверяем правильные ли параметры у команд
                            nameComand = list_comment[i][0].ToUpper(); //имя команды
                            coundParams = list_comment[i].Count - 1; //количество параметров

                            if (nameComand == "COM" && coundParams != 1 ||
                                nameComand == "FBG" && coundParams != 2 ||
                                nameComand == "FB1" && coundParams != 1 ||
                                nameComand == "FB2" && coundParams != 1 ||
                                nameComand == "FB3" && coundParams != 1)
                            {
                                isManualFormat = false;
                                break;
                            }
                        }

                        string[] parts_new = e.Message.ReplyToMessage.Text.Split(new char[] { '\n' });

                        if (isManualFormat)
                        {
                            string commentVisible = null;
                            bool haveKeyboard = false;
                            for (int i = 0; i < list_comment.Count; i++)
                            {
                                if (list_comment[i][0].ToUpper() == "COM")
                                {
                                    commentVisible = list_comment[i][1];
                                }
                                if (list_comment[i][0].ToUpper() != "COM" && SecretKey.comandComment.Contains(list_comment[i][0].ToUpper()))
                                {
                                    haveKeyboard = true;
                                }
                                if (commentVisible != null && haveKeyboard)
                                {
                                    break;
                                }
                            }
                            string format_text = getFormatTextNews(parts_new, commentVisible);

                            //если сообщение без клавиатуры
                            if (!haveKeyboard)
                            {
                                //отправляем сообщение
                                await botClient.SendTextMessageAsync(text: format_text,
                                                       chatId: SecretKey.CHANNEL,
                                                       parseMode: ParseMode.Html,
                                                       disableNotification: true,
                                                       disableWebPagePreview: false);

                                //удаляем исходное сообщение чтоб не засоряло чат
                                await botClient.DeleteMessageAsync(e.Message.From.Id, e.Message.MessageId);
                            }
                            //если клавиатура таки есть
                            else
                            {
                                InlineKeyboardMarkup keyboard = getKeyboard(list_comment);
                                //отправляем сообщение
                                await botClient.SendTextMessageAsync(text: format_text,
                                                       chatId: SecretKey.CHANNEL,
                                                       parseMode: ParseMode.Html,
                                                       disableNotification: true,
                                                       disableWebPagePreview: false,
                                                       replyMarkup: keyboard);

                                //удаляем исходное сообщение чтоб не засоряло чат
                                await botClient.DeleteMessageAsync(e.Message.From.Id, e.Message.MessageId);
                            }
                        }
                        //если в коменте одна строка - то это строка самого комментария, без команд форматирования поста
                        else if (rows_comment.Length == 1)
                        {
                            //формируем новый текст новости с комментарием
                            string format_text = getFormatTextNews(parts_new, comment);

                            //формируем кнопки к новости

                            InlineKeyboardMarkup keyboard = getKeyboard(null);

                            //отправляем сообщение
                            await botClient.SendTextMessageAsync(text: format_text,
                                                   chatId: SecretKey.CHANNEL,
                                                   parseMode: ParseMode.Html,
                                                   disableNotification: true,
                                                   disableWebPagePreview: false,
                                                   replyMarkup: keyboard);

                            //удаляем исходное сообщение чтоб не засоряло чат
                            await botClient.DeleteMessageAsync(e.Message.From.Id, e.Message.MessageId);

                        }
                        else
                        {
                            await botClient.DeleteMessageAsync(e.Message.From.Id, e.Message.MessageId);
                            await botClient.SendTextMessageAsync(text: "Неправильно заполнены параметры команды",
                                                   chatId: e.Message.From.Id,
                                                   parseMode: ParseMode.Html,
                                                   disableNotification: true,
                                                   disableWebPagePreview: false);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Произошел сбой при ручном задании формата новости");
                    Console.WriteLine($"Исключение: {ex.Message}");
                }
            }
        }

        //функция определяет добавляем ли мы комментарий к новости или создаем свой пост
        public static bool isCommentNews(MessageEventArgs e)
        {
            string[] words = e.Message.ReplyToMessage.Text.Split(new char[] { '\n' });
            if (words.Length == 6)
            {
                //получаем названия кнопок
                var textButton = e.Message.ReplyToMessage.ReplyMarkup.InlineKeyboard.ToList().ConvertAll(x => x.ToList());
                if (textButton.Count == 2)
                {
                    if (textButton[0][0].CallbackData == "standart_public_bot" && textButton[1][0].CallbackData == "delete_message_bot")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //функция возвращает форматированный текст новости
        public static string getFormatTextNews (string[] par_parts_new, string par_comment) 
        {
            string formatText;
            string type = par_parts_new[0];
            string title = par_parts_new[1];
            string short_text = par_parts_new[2];
            string picture = par_parts_new[3];
            string agency = par_parts_new[4];
            string href = par_parts_new[5];

            if(par_comment == null)
            {
                formatText = "#" + type + "\n" + "<b>" + title + "</b>" + "\n" + "\n" 
                            + short_text + "\n" + $"<a href=\"{picture}\">⁠⁠⁠⁠⁠⁠⁠</a>" + "\n" + $"{agency}: <a href=\"{href}\">Открыть новость</a>";
            }
            else
            {
                formatText = "#" + type + "\n" + "<b>" + title + "</b>" + "\n" + "\n"
                            + short_text + "\n" + "\n" + "<b>" + "<i>" + par_comment + "</i>" + "</b>" + "\n"
                            + $"<a href=\"{picture}\">⁠⁠⁠⁠⁠⁠⁠</a>" + "\n" + $"{agency}: <a href=\"{href}\">Открыть новость</a>";
            }

            return formatText;
        }

        //функция формирует формирует список кнопок к посту на основе команд
        public static InlineKeyboardMarkup getKeyboard(List<List<string>> par_list_comand)
        {
            //если в качестве параметра отправили null - значит вовзращаем стандартный keyboard - лайк дизлайк
            List<List<InlineKeyboardButton>> bottons = new List<List<InlineKeyboardButton>>();
            if (par_list_comand == null)
            {
                bottons.Add(new List<InlineKeyboardButton>());
                bottons[0].Add(new InlineKeyboardButton { Text = "👍", CallbackData = "like_message_bot" });
                bottons[0].Add(new InlineKeyboardButton { Text = "👎", CallbackData = "dislike_message_bot" });
            }
            else
            {
                int iter = 0;
                for(int i = 0; i < par_list_comand.Count; i ++)
                {
                    if(par_list_comand[i][0].ToUpper() == "FBG")
                    {
                        bottons.Add(new List<InlineKeyboardButton>());
                        bottons[iter].Add(new InlineKeyboardButton { Text = par_list_comand[i][1], CallbackData = "like_message_bot" });
                        bottons[iter].Add(new InlineKeyboardButton { Text = par_list_comand[i][2], CallbackData = "dislike_message_bot" });
                        iter++;
                    }

                    if(par_list_comand[i][0].ToUpper() == "FB1")
                    {
                        bottons.Add(new List<InlineKeyboardButton>());
                        bottons[iter].Add(new InlineKeyboardButton { Text = par_list_comand[i][1], CallbackData = "feedback_1_message_bot" });
                        iter++;
                    }

                    if (par_list_comand[i][0].ToUpper() == "FB2")
                    {
                        bottons.Add(new List<InlineKeyboardButton>());
                        bottons[iter].Add(new InlineKeyboardButton { Text = par_list_comand[i][1], CallbackData = "feedback_2_message_bot" });
                        iter++;
                    }

                    if (par_list_comand[i][0].ToUpper() == "FB3")
                    {
                        bottons.Add(new List<InlineKeyboardButton>());
                        bottons[iter].Add(new InlineKeyboardButton { Text = par_list_comand[i][1], CallbackData = "feedback_3_message_bot" });
                        iter++;
                    }
                }               
            }
            InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(bottons);

            return keyboard;
        }

        //public static void Method(object sender, InlineQueryEventArgs e)
        //{
        //    Console.WriteLine(e.InlineQuery.Query);
        //}

    }
}
