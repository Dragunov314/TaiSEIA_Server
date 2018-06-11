using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;
using TaiSEIA;

namespace Server_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            string port = "8080";

            TaiSEIA_Server server = new TaiSEIA_Server(port);


            server.StartConnect();
            while (server.clients.Count == 0) { }

            string[] input = new string[4];
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===================Input Form===================");
            Console.Write("Enter USER ID : ");
            input[0] = "2561";// Console.ReadLine();
            Console.Write("Enter HG ID : ");
            input[1] = "1";// Console.ReadLine();
            Console.Write("Enter HNA ID : ");
            input[2] = "2";// Console.ReadLine();
            Console.Write("Enter Security Type : ");
            input[3] = "0";// Console.ReadLine();
            server.clients[0].setClientData(input[0], input[1], input[2], input[3]);
            Console.WriteLine("Input Completed!!");
            Console.WriteLine("USER ID : "+input[0]+ ", HG ID : "+input[1]+
                              ", HNA ID : "+input[2]+", Security Type : "+ input[3]);
            Console.WriteLine("=================================================");
            Console.ResetColor();

            while (true)
            {
                string cmd;       
                Console.ResetColor();         
                Console.Write("TaiSEIA Server> ");
                
                cmd = Console.ReadLine();
                

                if (cmd.Equals("Q") || cmd.Equals("q"))
                    break;                                  
                else
                {
                    server.ServerSend(cmd);
                }
            }

            Console.WriteLine("\nTaiSEIA Server terminated.");
            Console.Write("\nPress any key to continue... ");
            Console.ReadLine();
        }
    }
}
