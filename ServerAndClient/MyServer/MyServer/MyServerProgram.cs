using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyServer {

    class MyServerProgram {
        public static string UTF8toASCII(string text) {
            System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
            Byte[] encodedBytes = utf8.GetBytes(text);
            Byte[] convertedBytes = Encoding.Convert(Encoding.UTF8, Encoding.ASCII, encodedBytes);
            System.Text.Encoding ascii = System.Text.Encoding.ASCII;

            return ascii.GetString(convertedBytes);
        }

        public static string readFromStream(NetworkStream stream, int receiveBufferSize) {
            string s = string.Empty;
            byte[] buffer = new byte[receiveBufferSize];
            int numberOfBytesRead = stream.Read(buffer, 0, buffer.Length);
            s = Encoding.UTF8.GetString(buffer);
            return s;
        }
        public static void writeToStream(NetworkStream stream, string message) {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
        }
        static void Main(string[] args) {
            TcpListener server = null;
            //IPAddress ipAdd = Dns.Resolve("localhost").AddressList[0];
            string hostname = "127.0.0.1";
            int port = 1997;

            try {
                //server = new TcpListener(IPAddress.Parse(hostname), port);
                server = new TcpListener(IPAddress.Parse(hostname), port);
                server.Start();
                Console.WriteLine("MyServer started...");

                while (true) {
                    Console.WriteLine();
                    Console.WriteLine("Waiting for incoming connenctions...");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Accepted a new client connection...");
                    StreamReader reader = new StreamReader(client.GetStream());
                    StreamWriter writer = new StreamWriter(client.GetStream());
                    NetworkStream netStream = client.GetStream();
                    string s = string.Empty;

                    while (!s.Equals("no") || (s == null)) {
                        s = reader.ReadLine();

                        //writer.WriteLine("From Server: {0}", TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(s)));
                        writer.WriteLine(DateTime.UtcNow);
                        //writer.WriteLine("From Server: {0}", TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time")));
                        writer.Flush();

                    }

                    reader.Close();
                    writer.Close();
                    client.Close();
                }
            }
            catch (SocketException e) {
                Console.WriteLine("SocketException: {0}", e.Message);
            }
            finally {
                if (server != null) {
                    server.Stop();
                }
            }

        }
    }
}
