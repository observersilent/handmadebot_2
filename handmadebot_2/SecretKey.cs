using System;
using System.Collections.Generic;
using System.Text;

namespace handmadebot_2
{
    public static class SecretKey
    {
        //токен от бота(бот имеет доступ и к чату и к каналу)
        public const string API_KEY = "857536457:AAEe4H-KSl5Njqc2f0o4YiOzOUKE8H-MCSc";
        //номер канала
        public const long CHANNEL = -1001466398761;
        //номер чата (чат подключен к каналу как обсуждение)
        public const long CHAT = -1001437588233;
        //идентификатор владельца (мой)
        public const int OWNER_ID = 896172479;

        public static List<string> comandComment = new List<string>() { "COM", //комментарий 
                                                                        "FBG", //лайки/дизлайки горизонтальное направление кнопок
                                                                        "FB1", //фидбек 1
                                                                        "FB2", //фидбек 2
                                                                        "FB3" }; //фидбек 3
    }
}
