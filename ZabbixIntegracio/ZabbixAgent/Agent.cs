using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using log4net;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using JSONClasses;

namespace ZabbixAgent {
    public class Agent : IAgent {
        public event EventHandler<ZabbixEventArgs> ZabbixEventHappened;
        private long epoch = DateTimeOffset.Now.ToUnixTimeSeconds();
        private Random rnd = new Random();

        /// <summary>
        /// Eltávolítja a NULL bájtokat az adott tömbből.
        /// </summary>
        /// <param name="data">Tömb, amelyből el akarjuk távolítani</param>
        /// <returns>NULL karakter nélküli bájttömb</returns>
        private byte[] RemoveNull(byte[] data) {
            bool dataFound = false;
            // skipwhile: átlépi a feltételre igaz elemeket, a többit visszaadja
            byte[] newData = data.Reverse().SkipWhile(index => {
                if (dataFound)
                    return false;
                if (index == 0x00)
                    return true;
                else {
                    dataFound = true;
                    return false;
                }
            }).Reverse().ToArray();
            return newData;
        }

        /// <summary>
        /// Ez a függvény kiolvassa a hálózati adatfolyamból az adatot egy bájt tömbbe.
        /// </summary>
        /// <param name="netStream">A hálózati hozzáféréshez biztosított adatfolyam</param>
        /// <param name="receiveBufferSize">Mennyi adatot olvasson ki egyszerre</param>
        /// <returns>Kiolvasott adat byte tömb formában</returns>
        private byte[] readFromStream(NetworkStream netStream, int receiveBufferSize) {
            byte[] buffer = new byte[receiveBufferSize];
            if (netStream.CanRead) {
                bool flag = false;
                while (!flag) {
                    if (netStream.DataAvailable) {
                        int length = netStream.Read(buffer, 0, buffer.Length);
                        flag = true;
                    }
                    else {
                        Console.WriteLine("The server has not sent response yet . . .\n");
                        Thread.Sleep(1000);
                    }
                }
            }
            else {
                Console.WriteLine("Sorry, you cannot read from the stream");
            }

            return RemoveNull(buffer);
        }

        /// <summary>
        /// Ez a függvény adatot küld a hálózati adatfolyamba
        /// </summary>
        /// <param name="stream">A hálózati hozzáféréshez biztosított adatfolyam</param>
        /// <param name="message">A küldeni kívánt üzenet</param>
        private void writeToStream(NetworkStream stream, byte[] message) {
            if (stream.CanWrite) {
                stream.Write(message, 0, message.Length);
                stream.Flush();
            }
            else {
                Console.WriteLine("Sorry, you cannot write to the stream");
            }
        }

        /// <summary>
        /// Ez a metódus előállítja "active checks" kérés üzenetét, ahol a "request" = "active checks", "host" = "gyakornok_ck_app". 
        /// </summary>
        /// <returns>A kérés üzenete string formában</returns>
        private string ActiveChecksRequest() {
            // példányosítás és az adott objektum json-né alakítása
            ActiveChecks ac = new ActiveChecks() {
                request = "active checks",
                host = "gyakornok_ck_app"
            };
            string json = JsonConvert.SerializeObject(ac, Formatting.Indented);
            return json;
        }

        /// <summary>
        /// Ez a függvény a Zabbix protokoll definíciójának megfelelően, Header és Datalen résszel látja el az elküldeni kívánt üzenetet.
        /// Datalen: little-endian biztos
        /// </summary>
        /// <param name="message">A küldeni kívánt üzenet</param>
        /// <returns>A végső üzenet byte tömb formában.</returns>
        private byte[] CreateHeaderAndDatalen(string message) {
            byte[] usefulData = Encoding.UTF8.GetBytes(message);
            byte[] header = Encoding.UTF8.GetBytes("ZBXD\x01");
            long usefulDataLength = Convert.ToInt64(usefulData.Length);
            byte[] usefulDataLengthToByte = BitConverter.GetBytes(usefulDataLength);
            byte[] finalMessage = new byte[usefulDataLength + 13];

            // copy header into request message
            Array.Copy(header, finalMessage, header.Length);

            // order of byte; copy data length into request message
            if (BitConverter.IsLittleEndian) {
                Array.Copy(usefulDataLengthToByte, 0, finalMessage, header.Length, usefulDataLengthToByte.Length);
            }
            else {
                Array.Reverse(usefulDataLengthToByte);
                Array.Copy(usefulDataLengthToByte, 0, finalMessage, header.Length, usefulDataLengthToByte.Length);
            }

            // copy useful data into request message
            Array.Copy(usefulData, 0, finalMessage, header.Length + usefulDataLengthToByte.Length, usefulData.Length);

            return finalMessage;
        }

        /// <summary>
        /// Ez a függvény eltávolítja a header és a datalen részt, és a megmaradt adatot kilogoljuk utf8 kódolásnak megfelelően fájlba.
        /// </summary>
        /// <param name="response">Az hálózati adatfolyamból kiolvasott adat</param>
        /// <returns>Header és datalen nélküli adat string formában.</returns>
        private string RemoveHeaderAndDatalen(byte[] response) {
            // összesen 13 bájt a header és a datalen mérete
            byte[] withoutHeadAndLen = new byte[response.Length - 13];

            // a 13. bájttól van a hasznos adat
            Array.Copy(response, 13, withoutHeadAndLen, 0, response.Length - 13);
            string finalMess = Encoding.UTF8.GetString(withoutHeadAndLen);

            ILog responseActiveChecksLogger = LogManager.GetLogger("responseActiveChecks");
            responseActiveChecksLogger.Debug(finalMess);

            return finalMess;
        }

        /// <summary>
        /// Ez a metódus feldolgozza az "active checks" kéréssel bejövő JSON üzenetet az "DeserializeAnonymousType" metódus segítségével,
        /// és kimenti a kulcsokat egy listába. Vmint kilogolja egy fájlba őkmet.
        /// </summary>
        /// <param name="finalMess">Header és datalen nélküli JSON üzenet</param>
        /// <param name="keys">Lista, ahova mentjük a kulcsokat</param>
        private void DeserializeJSON(string finalMess, out List<string> keys) {
            //var jActiveChecks = JsonConvert.DeserializeObject<dynamic>(finalMess);
            // konkrét objektum helyett, egy helyben definiált névtelen osztályt (tmp) használ fel sablonként, és ez alapján deszerializálja a json literált
            // az out-al több visszatérési értéke lehet az alprogramnak
            var tmp = new {
                response = string.Empty,
                data = new[] {
                    new { key = string.Empty, delay = string.Empty, lastlogsize = 0, mtime = 0 }
                }
            };
            var jActiveChecks = JsonConvert.DeserializeAnonymousType(finalMess, tmp);

            //var jActiveChecks = JsonConvert.DeserializeAnonymousType(File.ReadAllText(@"response.json"), tmp);
            keys = new List<string>();
            foreach (var item in jActiveChecks.data) {
                keys.Add(item.key);
            }

            ILog keyLogger = LogManager.GetLogger("keyLogger");
            keyLogger.Debug(string.Join(", ", keys.ToArray()));
        }

        /// <summary>
        /// Kiváltja ZabbixEventHappened eseményt.
        /// </summary>
        /// <param name="args">Az eseményhez kapcsolódó információk.</param>
        private void OnEvent(ZabbixEventArgs args) {
            if (ZabbixEventHappened != null)
                ZabbixEventHappened(this, args);

        }

        /// <summary>
        /// Ez a függvény az "active checks" kérésből kinyert kulcsokhoz a megfelelő értéket szolgáltatja vissza
        /// az OnEvent metódus meghívásával, ami a ZabbixEventHappened eseményt váltja ki.
        /// A kulcsokat átadja az eseményhez kapcsolódó key argumentumnak.
        /// </summary>
        /// <param name="key">A kulcs</param>
        /// <returns>Kulcshoz tartozó érték object típusként</returns>
        private object KeyToValue(string key) {
            ZabbixEventArgs args = new ZabbixEventArgs { key = key };
            OnEvent(args);
            return args.value;
        }

        /// <summary>
        /// Ez a függvény Dictionary kollekciót megvalósítva, kulcs-érték párt készít az adott kulcshoz tartozó értékkel.
        /// </summary>
        /// <param name="keys">Az a lista, amely a kulcsokat tartalmazza</param>
        /// <returns>Kulcs-érték párok kollekciója </returns>
        // object: a value lehet int, float, string, (text?, log?)
        private Dictionary<string, object> CreateKeyValuePairs(List<string> keys) {
            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            foreach (string item in keys) {
                keyValuePairs.Add(item, KeyToValue(item));
            }
            return keyValuePairs;
        }

        /// <summary>
        /// Ez a függvény egy Dictionary kollekció kulcs-érték párjaiból JSON objektumot készít az "active checks" elemeire válaszként.
        /// </summary>
        /// <param name="keyValuePairs">Kulcs-érték párokat tartalmazó kollekció</param>
        /// <returns>JSON objektum</returns>
        private string KeysAndValuesToJSON(Dictionary<string, object> keyValuePairs) {
            List<AgentData.Data> data = new List<AgentData.Data>();

            foreach (var item in keyValuePairs) {
                data.Add(new AgentData.Data() { host = "gyakornok_ck_app", key = item.Key, value = item.Value, clock = epoch, ns = 0 });
            }

            AgentData ad = new AgentData() { request = "agent data", data = data, clock = epoch, ns = 0 };

            string json = JsonConvert.SerializeObject(ad, Formatting.Indented);

            return json;
        }

        public void Start() {
            int port = 10051;
            string hostnameToWhichConnect = AgentSettings.Default.hostnameToWhichConnect;
            TcpClient client;
            NetworkStream netStream;

            //log4net.Config.XmlConfigurator.Configure();
            ILog errLogger = LogManager.GetLogger("error");
            ILog verifiedReply = LogManager.GetLogger("verifiedReply");
            List<string> keys;

            try {
                do {
                    using (client = new TcpClient(hostnameToWhichConnect, port))
                    using (netStream = client.GetStream()) {
                        netStream.Flush();

                        writeToStream(netStream, CreateHeaderAndDatalen(ActiveChecksRequest()));

                        Console.WriteLine("\nWaiting A . . . \n");

                        byte[] response = readFromStream(netStream, 1024);

                        DeserializeJSON(RemoveHeaderAndDatalen(response), out keys);

                        client = null;
                        client = new TcpClient(hostnameToWhichConnect, port);
                        netStream = client.GetStream();

                        writeToStream(netStream, CreateHeaderAndDatalen(KeysAndValuesToJSON(CreateKeyValuePairs(keys))));

                        Console.WriteLine("\nWaiting B. . . \n");

                        response = readFromStream(netStream, 1024);
                        verifiedReply.Debug(RemoveHeaderAndDatalen(response));
                    }
                    client = null;
                }
                while (true);
            }
            catch (Exception e) {
                errLogger.Error("An error happened: {0}", e);
            }
        }
    }
}
