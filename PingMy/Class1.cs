using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace PingMy
{
   static public class Class1
    {
        public static async bool LocalPing(string ip2)//Сканер адресов IP
        {

            try
            {
                //создаем класс Ping 
                Ping ClassPing = new Ping();
                //выполняем команду и сохраняем результат в переменную Status
                PingReply Status = ClassPing.Send(ip2, 200);
                //если статус = Success сохраняем правду иначе оставим ложь
                if (Status.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
