using System;
using System.Linq;
using PassiveScanning.ScansIo;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PassiveScanning
{
    class MainClass
    {
        public static HostList HostList;

        public static void Main(string[] args)
        {
            if (Directory.Exists("output"))
                Directory.Delete("output", true);
            Directory.CreateDirectory("output");

            ThreadPool.SetMaxThreads(2, 1);
            ThreadPool.SetMinThreads(1, 1);

            Console.WriteLine("Loading list of dutch hosts...");

            HostList = new HostList("nl.csv");
            Console.WriteLine("Found {0} dutch hosts.", HostList.Hosts.Count);

            FindServiceDescriptor[] services = new FindServiceDescriptor[]
            { 
                new FindServiceDescriptor(143, "IMAP", "5elhwfrqv15nq5px-143-imap-starttls-full_ipv4-20150617T163103-zgrab-results.json", "5elhwfrqv15nq5px-143-imap-starttls-full_ipv4-20150617T163103-zmap-results.csv"),
                new FindServiceDescriptor(21, "FTP", "7ngdfqqrhmqdce38-21-ftp-banner-full_ipv4-20150801T233003-zgrab-results.json", "7ngdfqqrhmqdce38-21-ftp-banner-full_ipv4-20150801T233003-zmap-results.csv"),
                new FindServiceDescriptor(995, "POP3S", "gf1z452301hyhs3w-995-pop3s-tls-full_ipv4-20150802T140000-zgrab-results.json", "gf1z452301hyhs3w-995-pop3s-tls-full_ipv4-20150802T140000-zmap-results.csv"),
                new FindServiceDescriptor(443, "Heartbleed", "ju8g62b9picx0i3i-443-https-heartbleed-full_ipv4-20150706T000000-zgrab-results.json", "ju8g62b9picx0i3i-443-https-heartbleed-full_ipv4-20150706T000000-zmap-results.csv"),
                new FindServiceDescriptor(25, "SMTP", "klnqp1y00vooeonh-25-smtp-starttls-full_ipv4-20150803T040000-zgrab-results.json","klnqp1y00vooeonh-25-smtp-starttls-full_ipv4-20150803T040000-zmap-results.csv"),
                new FindServiceDescriptor(993, "IMAPS", "pt15h1gy6uic493j-993-imaps-tls-full_ipv4-20150721T120000-zgrab-results.json", "pt15h1gy6uic493j-993-imaps-tls-full_ipv4-20150721T120000-zmap-results.csv"),
                new FindServiceDescriptor(443, "HTTPS", "ydns0pmlsiu0996u-443-https-tls-full_ipv4-20150804T010006-zgrab-results.json", "ydns0pmlsiu0996u-443-https-tls-full_ipv4-20150804T010006-zmap-results.csv"),
                new FindServiceDescriptor(110, "POP3", "z2nk2bbxgipkjl9k-110-pop3-starttls-full_ipv4-20150729T221221-zgrab-results.json", "z2nk2bbxgipkjl9k-110-pop3-starttls-full_ipv4-20150729T221221-zmap-results.csv")
            };

            foreach (var service in services)
                ThreadPool.QueueUserWorkItem(FindServices, service);

            Console.WriteLine("Synchronizing threads...");
            foreach (var service in services)
                service.WaitHandle.WaitOne();

            Console.WriteLine("Serializing Hosts...");

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("Hosts.bin", FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, HostList.Hosts);
            stream.Close();

            Console.WriteLine("Done.");
        }

        public static void FindServices(object state)
        {
            FindServiceDescriptor findServiceDescriptor = (FindServiceDescriptor)state;

            try
            {
                Console.WriteLine("Loading ZMAP {0}-Banner results...", findServiceDescriptor.Name);

                ZmapResults mapResults = new ZmapResults(findServiceDescriptor.ZmapResultsPath, HostList);
                Console.WriteLine("Found Dutch {0} hosts with {1}.", mapResults.Addresses.Length, findServiceDescriptor.Name);

                Console.WriteLine("Fetching banners for Dutch {0} hosts...", findServiceDescriptor.Name);
                ZgrabResults grabResults = new ZgrabResults(findServiceDescriptor.Port, findServiceDescriptor.Name, findServiceDescriptor.ZgrabResultsPath, HostList, mapResults.Addresses);
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception occurred while finding services: {0}.", e.ToString());
            }
            finally
            {
                findServiceDescriptor.WaitHandle.Set();
            }
        }
    }
}