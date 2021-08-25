using System;
using System.Net.Sockets;
using System.IO;
using System.Text;


namespace MyClient {
    class MyClientProgram {

        //US Eastern Standard Time (GMT-5) | Central Europe Standard Time (GMT+1)| GTB Standard Time (GMT+2)| Argentina Standard Time (GMT-3)| AUS Eastern Standard Time(GMT+10) |Tokyo Standard Time (GMT+9)

        // streambol olvasunk
        public static string readFromStream(NetworkStream stream, int receiveBufferSize) {
            string s = string.Empty;
            byte[] buffer = new byte[receiveBufferSize];
            stream.Read(buffer, 0, buffer.Length);
            s = Encoding.UTF8.GetString(buffer);
            return s;
        }

        // streambe irunk
        public static void writeToStream(NetworkStream stream, string message) {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
        }

        // kiirjuk az egyes varosokban levo idot UTC-kent az eltelodas ertekevel
        public static void writeTimeInTowns(DateTime dateTimeFromServer) {
            string[] timeZones = { "US Eastern Standard Time", "Argentina Standard Time", "Greenwich Standard Time", "Central Europe Standard Time", "Tokyo Standard Time" };
            string[] towns = { "New York", "Buenos Aires", "London", "Budapest", "Tokyo" };
            int i = 0;
            
            foreach (string id in timeZones) {
                DateTime newDate = TimeZoneInfo.ConvertTimeFromUtc(dateTimeFromServer, TimeZoneInfo.FindSystemTimeZoneById(id));
                if (TimeZoneInfo.FindSystemTimeZoneById(id).BaseUtcOffset.TotalSeconds >= 0) {
                    Console.WriteLine("{0}+{1}\tin {2}", newDate.ToString("r"), TimeZoneInfo.FindSystemTimeZoneById(id).BaseUtcOffset, towns[i]);
                    i++;
                }
                else {
                    Console.WriteLine("{0}{1}\tin {2}", newDate.ToString("r"), TimeZoneInfo.FindSystemTimeZoneById(id).BaseUtcOffset, towns[i]);
                    i++;
                }
            }
        }
        // kiirjuk a rendszeridot UTC-kent az eltelodas ertekevel
        public static void writeOwnSystemTime(DateTime dateTimeFromServer) {
            TimeSpan diffTime = DateTimeOffset.Now.Offset;
            double totalSec = DateTimeOffset.Now.Offset.TotalSeconds;
            DateTime newDate = DateTime.Now;

            if (totalSec >= 0) {
                Console.WriteLine("{0}+{1}\tin your System", newDate.ToString("r"), diffTime);
            }
            else {
                Console.WriteLine("{0}{1}\tin your System", newDate.ToString("r"), diffTime);
            }
        }

        static void Main(string[] args) {
            int port = 1997;
            string hostname = "127.0.0.1";
            try {
                TcpClient client = new TcpClient(hostname, port);
                NetworkStream netStream = client.GetStream();
                StreamReader reader = new StreamReader(client.GetStream());
                StreamWriter writer = new StreamWriter(client.GetStream());
                string s = string.Empty;
                
                while (!s.Equals("no")) {
                    Console.WriteLine("Do you want to know the time? yes/no ");
                    s = Console.ReadLine();
                    Console.WriteLine();
                    writer.WriteLine(s);

                    writer.Flush();
                    netStream.Flush();

                    string serverString = readFromStream(netStream, 1024);

                    try {
                        DateTime dateTimeFromServer = Convert.ToDateTime(serverString);
                        
                        writeTimeInTowns(dateTimeFromServer);
                        writeOwnSystemTime(dateTimeFromServer);
                        Console.WriteLine();

                        //Console.WriteLine(TimeZoneInfo.ConvertTimeFromUtc(dateTimeFromServer, TimeZoneInfo.FindSystemTimeZoneById("US Eastern Standard Time")));
                    }
                    catch (Exception e) {
                        if (!s.Equals("no")) {
                            Console.WriteLine("Unable to convert: {0} {1}", serverString, e);
                        } 
                    }
                }
                writer.Close();
                reader.Close();
                client.Close();
                netStream.Close();
            }
            catch (SocketException e) {
                Console.WriteLine("SocketException: {0}", e.Message);
            }
        }
    }
}
