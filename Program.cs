using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System;
using System.Security.Cryptography;

namespace UDPClient
{
    class Program
    {
        public class Vars
        {
            public static string srvHash = "";
            public static int srvPos = 0;
            public static int srvInc = 1000000;
            public static int curPos = 0;
            public static string srvIP = "192.168.1.107";
        }

        static void Main(string[] args)
        {
            Thread Thread1 = null;  // create thread instance
            UdpClient udpClient = new UdpClient(8008);

            Byte[] recieveBytes = new Byte[1024]; // buffer to read the data into 1 kilobyte at a time
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 8008);  //open port 8008 on this machine
            Console.WriteLine("Client has Started");
            
            //recieve the data from the UDP packet
            recieveBytes = udpClient.Receive(ref remoteIPEndPoint);
            Vars.srvHash = Encoding.ASCII.GetString(recieveBytes);
            Console.WriteLine(Vars.srvHash); //hash debug

            Thread1 = new Thread(new ThreadStart(Crack)); //ascociate the function with the thread
            Thread1.Start();

            Console.WriteLine("It's Crack Time, Warm The Pipe Up");
            Console.ReadLine(); //delay end of program
            Thread1.Abort();
            udpClient.Close();  //close the connection
            Environment.Exit(0); //kill the application and all threads
        }

        static void Crack()
        {
            //get the position from the server
            UdpClient udpClient2 = new UdpClient(8009);
            string returnData = "";

            Byte[] recieveBytes = new Byte[1024]; // buffer to read the data into 1 kilobyte at a time
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 8009);  //open port 8009 on this machine

            recieveBytes = udpClient2.Receive(ref remoteIPEndPoint);
            returnData = Encoding.ASCII.GetString(recieveBytes);

            Vars.srvPos = Convert.ToInt32(returnData);
            Console.WriteLine("Current Position From Server: " + Vars.srvPos); //position recieved from server

            udpClient2.Close();
            returnPos("next"); //if a client connects in the middle of this, they get the next set of work

            for (int j = Vars.srvPos; j <= (Vars.srvPos + Vars.srvInc); j++)
            {
                //calculate MD5 hash from input
                MD5 md5 = MD5.Create();
                byte[] inputBytes = Encoding.ASCII.GetBytes(j.ToString());
                byte[] hash = md5.ComputeHash(inputBytes);

                //convert byte array to hex string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                StringComparer comp = StringComparer.OrdinalIgnoreCase;
                if (0 == comp.Compare(Vars.srvHash, sb.ToString()))
                {
                    Console.WriteLine("Hash Found: " + j.ToString());
                    returnPos("found");
                    break;
                }
                if (j % 100000 == 0)
                {
                    Console.WriteLine("Cracking Position: " + j.ToString());
                }
                Vars.curPos = (j);
            }
            Console.WriteLine("Hash Not Yet Found. Last Position: " + Vars.curPos.ToString());
            //Vars.srvPos = Vars.curPos;
            Crack();
        }

        static void returnPos(string curPos) //make it a string
        {
            UdpClient sender = new UdpClient();
            Byte[] sendBytes = new Byte[1024];
            IPAddress address = IPAddress.Parse(IPAddress.Broadcast.ToString());
            sender.Connect(address, 8010);

            sendBytes = Encoding.ASCII.GetBytes(curPos.ToString());
            sender.Send(sendBytes, sendBytes.GetLength(0));
        }
    }
}
