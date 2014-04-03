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
        public class Vars //global variables
        {
            public static string srvHash = "";
            public static int srvPos = 0;
            public static int srvInc = 1000000;
            public static int curPos = 0;
        }

        static void Main(string[] args)
        {
            try
            {
                Thread crackThread = null;  //create thread instance
                receiveHash();
                Console.WriteLine("Client has Started");

                crackThread = new Thread(new ThreadStart(Crack)); //ascociate the function with the thread
                crackThread.Start();

                Console.WriteLine("It's Crack Time, Warm The Pipe Up");
                Console.ReadLine(); //delay end of program

                crackThread.Abort();
                Environment.Exit(0); //kill the application and all threads
            }
            catch
            {
                Console.WriteLine("Error in Main Thread.");
            }
        }

        static void receiveHash()
        {
            try
            {
                UdpClient hashReciever = new UdpClient(8008);
                Byte[] recieveBytes = new Byte[1024]; //buffer to read the data into 1 kilobyte at a time
                IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 8008);  //open port 8008 on this machine

                recieveBytes = hashReciever.Receive(ref remoteIPEndPoint);
                Vars.srvHash = Encoding.ASCII.GetString(recieveBytes);
                hashReciever.Close();  //close the connection
                if (Vars.srvHash == "found")
                {
                    Environment.Exit(0);
                }
            }
            catch
            {
                Console.WriteLine("Receiving Hash Failed.");
            }
        }

        static void Crack()
        {
            try
            {

                //get the position from the server
                UdpClient posReceiver = new UdpClient(8009);
                string receivedPos = "";

                Byte[] recievedBytes = new Byte[1024];
                IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 8009);

                recievedBytes = posReceiver.Receive(ref remoteIPEndPoint);
                receivedPos = Encoding.ASCII.GetString(recievedBytes);

                Vars.srvPos = Convert.ToInt32(receivedPos); //position recieved from server
                Console.WriteLine("Current Position From Server: " + Vars.srvPos);

                posReceiver.Close();
            }
            catch
            {
                Console.WriteLine("Failed Recieving Position From Server.");
            }

            try
            {
                returnPos("next"); //makes the server increment the crackingPos
            }
            catch
            {
                Console.WriteLine("Failed to Return Position to Server.");
            }

            try
            {
                for (int j = Vars.srvPos; j <= (Vars.srvPos + Vars.srvInc); j++)
                {
                    //calculate MD5 hash from input
                    MD5 md5 = MD5.Create();
                    byte[] hashThis = Encoding.ASCII.GetBytes(j.ToString());
                    byte[] hash = md5.ComputeHash(hashThis);

                    //convert bytes to string for comparison
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++)
                    {
                        sb.Append(hash[i].ToString("x2"));
                    }
                    StringComparer md5Comp = StringComparer.OrdinalIgnoreCase;
                    if (0 == md5Comp.Compare(Vars.srvHash, sb.ToString()))
                    {
                        Console.WriteLine("Hash Found: " + j.ToString());
                        returnPos("found:" + j.ToString());
                        receiveHash(); //check if the hash has been found somewhere else
                        Thread.Sleep(10000);
                        Thread.CurrentThread.Abort();
                    }
                    if (j % 100000 == 0) //output to console every 10K
                    {
                        Console.WriteLine("Cracking Position: " + j.ToString());
                    }
                    Vars.curPos = (j);
                }
            }
            catch
            {
                Console.WriteLine("Failed in the MD5 Hash Creating/Matching");
            }
            receiveHash(); //check if the hash has been found somewhere else
            Crack();
        }

        static void returnPos(string strSend) //make it a string
        {
            try
            {
                UdpClient posSender = new UdpClient();
                Byte[] sendBytes = new Byte[1024];
                IPAddress address = IPAddress.Parse(IPAddress.Broadcast.ToString());
                posSender.Connect(address, 8010);

                sendBytes = Encoding.ASCII.GetBytes(strSend.ToString());
                posSender.Send(sendBytes, sendBytes.GetLength(0));
            }
            catch
            {
                Console.WriteLine("Failed to Return Position to Client.");
            }
        }
    }
}
