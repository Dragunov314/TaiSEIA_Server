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
            Console.WriteLine("===================Input Form==============================\n");
            Console.Write("Enter USER ID : ");
            input[0] = "2561";// Console.ReadLine();//
            Console.Write("Enter HG ID : ");
            input[1] = "1";//Console.ReadLine();//
            Console.Write("Enter HNA ID : ");
            input[2] = "2";// Console.ReadLine();//
            Console.Write("Enter Security Type : ");
            input[3] = "0";//Console.ReadLine();// 
            //server.clients[0].setClientData(input[0], input[1], input[2], input[3]);
            Console.WriteLine("Input Completed!!");
            Console.WriteLine("USER ID : "+input[0]+ ", HG ID : "+input[1]+
                              ", HNA ID : "+input[2]+", Security Type : "+ input[3]);
            Console.WriteLine("\n===========================================================");
            Console.ResetColor();

            while (true)
            {
                string cmd;       
                Console.ResetColor();         
                Console.Write("TaiSEIA Server> ");
                
                cmd = Console.ReadLine();
                
                if (cmd.Equals("list"))
                {
                    server.printStatus();
                }
                else if (cmd.Equals("goto"))
                {
                    string par;                    
                    Console.Write("     Client Number>> ");
                    par = Console.ReadLine();
                    int cur_client = Convert.ToInt32(par);

                    while (server.clients[cur_client].cln_socket.Connected==true)
                    {
                        Console.ResetColor();
                        Console.Write(par+"> ");
                        cmd = Console.ReadLine();
                        if (cmd.Equals("Q") || cmd.Equals("q"))
                            break;
                        else if (cmd.Equals("open"))
                        {
                            server.clients[cur_client].sendFCN2Client(0x0400, new string[] { "1", "0x00", "1" });
                        }
                        else if (cmd.Equals("close"))
                        {
                            server.clients[cur_client].sendFCN2Client(0x0400, new string[] { "1", "0x00", "0" });
                        }
                        else if (cmd.Equals("test"))
                        {
                            server.clients[cur_client].sendFCN2Client(0x0400, new string[] { "1", "0x01", "3" });
                        }
                        else if (cmd.Equals("printSA"))
                        {
                            server.clients[cur_client].paired_SA.printSupportList();
                        }
                        else if (cmd.Equals("write"))
                        {
                            string par1, par2;
                            Console.Write("     Input Function ID>> ");
                            par1 = Console.ReadLine();
                            Console.Write("     Input Data Value>> ");
                            par2 = Console.ReadLine();
                            server.clients[cur_client].sendFCN2Client(0x0400, new string[] { "1", par1, par2 });
                        }
                        else if (cmd.Equals("read"))
                        {
                            string par1;
                            Console.Write("     Input Function ID>> ");
                            par1 = Console.ReadLine();
                            server.clients[cur_client].sendFCN2Client(0x0400, new string[] { "0", par1, "0" });//0xFFFF
                        }
                        else
                        {
                            Console.WriteLine("No command matched..!!");
                        }
                    }  
                    if(server.clients[cur_client].cln_socket.Connected == false)                  
                        Console.WriteLine("Client "+ cur_client +" went offline!!");                    
                }
                else if (cmd.Equals("Q") || cmd.Equals("q"))
                    break;                                  
                
            }

            Console.WriteLine("\nTaiSEIA Server terminated.");
            Console.Write("\nPress any key to continue... ");
            Console.ReadLine();
        }
    }
}
