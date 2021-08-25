using System;
using System.IO;
using System.Text;
using System.Threading;
using ZabbixAgent;

namespace KulsoProgram {
    class Program {
        private static Random rnd = new Random();
        static void Main(string[] args) {
            IAgent agent = new Agent();
            agent.ZabbixEventHappened += Agent_ZabbixEventHappened;
            
            ThreadPool.QueueUserWorkItem((temp) => { agent.Start(); });
            Console.ReadLine();
        }

        /// <summary>
        /// Ez a függvény egy random stringet készít '\x20' és a '\x7e' közötti karakterekből.
        /// </summary>
        /// <param name="strLen">A string karakterszáma</param>
        /// <returns>Random string</returns>
        private static string GenerateRandomText(int strLen) {
            char rndChar;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < strLen; i++) {
                rndChar = (char)rnd.Next('\x20', '\x7e');
                sb.Append(rndChar);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Ez a függvény az agent objektum által kiváltott eseményét kezeli le. 
        /// Az "active checks" kérésből kinyert kulcsoknak megfelelő értéket generálja, 
        /// amiket hozzárendel az eseményhez tartozó value argumentumhoz.
        /// Pl: Az "agent.hostname" kulcsra a "gyakornok_ck_app" értéket.
        /// </summary>
        /// <param name="sender">Az az objektum, mely kiváltotta az eseményt</param>
        /// <param name="args">Az eseményhez kapcsolódó információk</param>
        private static void Agent_ZabbixEventHappened(object sender, ZabbixEventArgs args) {
            
            switch (args.key) {
                case "agent.ping":
                    args.value = 1.ToString();
                    break;
                case "gyakornok.numeric_float":
                    args.value = rnd.NextDouble().ToString().Replace(",", ".");
                    break;
                case "agent.hostname":
                    args.value = "gyakornok_ck_app";
                    break;
                case "agent.version":
                    args.value = "1.0.0";
                    break;
                case "gyakornok.character":
                    char ch = (char)rnd.Next(33, 126);
                    args.value = ch.ToString();
                    break;
                case "gyakornok.log":
                    args.value = File.ReadAllText(Path.GetFullPath(@"Log\Active_Checks\key\key.log.1"));
                    break;
                case "gyakornok.numeric_unsigned":
                    args.value = rnd.Next().ToString();
                    break;
                case "gyakornok.text":
                    args.value = GenerateRandomText(10);
                    break;
                default:
                    args.value = string.Empty;
                    break;
            }
        }
    }
}
