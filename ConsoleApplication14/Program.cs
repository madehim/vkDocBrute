using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace test1234
{
    class Program
    {
        public static int id = 1;
        public static int minrange = 0;
        public static int maxrange = 455000000;
        public static int num = 455000000;
        public static bool flagStop = false;

        public static List<string> skipedPages = new List<string>();
        public static List<string> threadlist = new List<string>();
        public static Timer ProxyCheckTimer; //check dead proxy
        public static Mutex threadListMutex = new Mutex();
        public static Mutex skipedPagesMutex = new Mutex();


        static void Main(string[] args)
        {
            Console.WriteLine("Команды id, minrange, maxrange, start, help, exit");
            string cmd = Console.ReadLine();
            


            while (true) //console menu stuff
            {
                switch (cmd)
                {
                    case "id":
                        Console.WriteLine("Введите id");
                        id = Convert.ToInt32(Console.ReadLine());
                        cmd = Console.ReadLine();
                        break;
                    case "minrange":
                        Console.WriteLine("Введите minrange");
                        minrange = Convert.ToInt32(Console.ReadLine());
                        cmd = Console.ReadLine();
                        break;
                    case "maxrange":
                        Console.WriteLine("Введите maxrange");
                        num = Convert.ToInt32(Console.ReadLine());
                        cmd = Console.ReadLine();
                        break;
                    case "start":
                        if (threadlist.Count == 0)
                            flagStop = false;
                        if (!flagStop)
                        {
                            maxrange = num;
                            Thread tr = new Thread(start);
                            tr.Start(id);
                        }
                        else
                            Console.WriteLine("Подождите остановки потоков");
                        cmd = Console.ReadLine();
                        break;
                    case "help":
                        Console.WriteLine("Вводим id, прописываем start. stop, exit для завершения");
                        cmd = Console.ReadLine();
                        break;
                    case "threadlist":
                        foreach (string thread in threadlist)
                            Console.WriteLine(thread);
                        cmd = Console.ReadLine();
                        break;
                    case "status":
                        Console.WriteLine(DateTime.Now.ToString() + " проверено " + (maxrange - num).ToString() + " из " +(maxrange - minrange).ToString() + ", количество потоков - " + threadlist.Count);
                        cmd = Console.ReadLine();
                        break;
                    case "exit":
                        Environment.Exit(0);
                        break;
                    case "stop":
                        flagStop = true;
                        cmd = Console.ReadLine();
                        break;
                    default:
                        Console.WriteLine("Неизвестная команда");
                        cmd = Console.ReadLine();
                        break;
                }

            }
        }


        //not used
        public static void consoleId(string get)
        {
            if (get == "id")
                Console.WriteLine("текущйи id - " + id.ToString() + ", для изменения пропищите id num, где num новый id в численном представлении");
            else
            {
                Char delimiter = ' ';
                string[] substring = get.Split(delimiter);
            }
        }










        public static void start(object id)
        {
            string[] proxylistfromfile = null; //get proxy list
            if (System.IO.File.Exists("proxylist.txt"))
                proxylistfromfile = System.IO.File.ReadAllLines("proxylist.txt");

            proxylistfromfile = CheckProxyFile(proxylistfromfile);

            Console.WriteLine("Proxy in file - " + proxylistfromfile.Length);

            

            //looking for alive proxy, if list exist
            if (proxylistfromfile != null)
            {
               
                string[] proxylist = new string[proxylistfromfile.Length];
                int k = 0;
                for (int i = 0; i < proxylistfromfile.Length; i++)
                {
                    if (ProxyStatusCheck(proxylistfromfile[i]))
                    {
                        proxylist[k] = proxylistfromfile[i];
                        k++;
                    }
                }
                Console.WriteLine("Alive proxy - " + (k + 1));

                
                //start thread with alive proxy
                for (int i = 0; i < k; i++)
                {
                    Thread tr = new Thread(ProxyThread);
                    tr.Name = proxylist[i];
                    tr.Start(proxylist[i]);
                    threadlist.Add(proxylist[i]);
                }
            }
            Thread tre = new Thread(ProxyThread); //start own pc thread
            tre.Name = "own pc";
            tre.Start("0");
            ProxyCheckTimer = new Timer(CheckProxyTimerCallback, null, 0, 1200 * proxylistfromfile.Length);
            
        }

        


        static void ProxyThread(object obj)
        {
            string htmlstr = "";
            Char delimiter = ':';
            string[] substring = obj.ToString().Split(delimiter);
            try
            {
                Mutex mtNum = new Mutex();

                WebClient client = new WebClient();//start webclient with/without proxy
                WebProxy wp = new WebProxy(obj.ToString());
                if (obj.ToString() != "0")
                    client.Proxy = wp;

                wp.UseDefaultCredentials = true;//idk why its here

                

                while (true) //check every page with step down
                {

                    

                    mtNum.WaitOne();
                    if (num == minrange) //check for end
                    {
                        if (threadlist.Count != 1)
                        {
                            threadlist.Remove(Thread.CurrentThread.Name);
                            Console.WriteLine("Thread " + Thread.CurrentThread.Name + " stopped, " + threadlist.Count + " still active");
                        }
                        else
                        {
                            threadlist.Remove(Thread.CurrentThread.Name);
                            Console.WriteLine("Last thread " + Thread.CurrentThread.Name + " stopped. ");
                            flagStop = false;
                        }
                        break;
                    }

                    string htmlstr1 = "";
                    string strtmp = "";
                    skipedPagesMutex.WaitOne();
                    if (skipedPages.Count != 0)//choose dec or "skiped page". second mean that some exception can happened when cheked this page before
                    {
                        htmlstr1 = skipedPages[0];
                        skipedPages.Remove(skipedPages[0]);
                    }
                    else
                    {
                        strtmp = num.ToString();
                        num--;
                    }
                    skipedPagesMutex.ReleaseMutex();

                    
                    mtNum.ReleaseMutex();


                    if (htmlstr1 == "")//create link for number
                    {
                        if (strtmp.Length < 9)
                            for (int j = strtmp.Length; j < 9; j++)
                                strtmp = "0" + strtmp;
                        htmlstr = "https://vk.com/doc" + id + "_" + strtmp;
                    }
                    else
                        htmlstr = htmlstr1;

                    string htmlCode = client.DownloadString(htmlstr);//get page

                    //Console.WriteLine(htmlCode);
                    if (!htmlCode.Contains("Файл был удалён") && !htmlCode.Contains("File deleted"))//check for file on page. mb isnt good solution
                    {
                        if (htmlCode.Contains("Этот документ был удалён из общего доступа") || htmlCode.Contains("This document is available only to its owner"))
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + " " + htmlstr + " only owner");
                            StreamWriter sw = new StreamWriter(id + "found_only_owner.txt", true);
                            sw.WriteLine(DateTime.Now.ToString() + " " + Thread.CurrentThread.Name + " " + htmlstr);
                            sw.Close();
                        }
                        else
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + " " + htmlstr + " got something!");
                            StreamWriter sw = new StreamWriter(id + "found_something.txt", true);
                            sw.WriteLine(DateTime.Now.ToString() + " " + Thread.CurrentThread.Name + " " + htmlstr);
                            sw.Close();
                        }
                    }


                    if (flagStop)//check for stop by user. not all msg writed to console
                    {
                        threadListMutex.WaitOne();
                        if (threadlist.Count != 1)
                        {
                            threadlist.Remove(Thread.CurrentThread.Name);
                            Console.WriteLine("Thread " + Thread.CurrentThread.Name + " stopped, " + threadlist.Count + " still active");
                        }
                        else
                        {
                            threadlist.Remove(Thread.CurrentThread.Name);
                            Console.WriteLine("Last thread " + Thread.CurrentThread.Name + " stopped. ");
                            flagStop = false;
                        }
                        threadListMutex.ReleaseMutex();
                        break;
                    }
                }

            }
            catch (Exception e)
            {
                threadListMutex.WaitOne();
                threadlist.Remove(Thread.CurrentThread.Name);
                threadListMutex.ReleaseMutex();

                skipedPagesMutex.WaitOne();
                skipedPages.Add(htmlstr);
                skipedPagesMutex.ReleaseMutex();

                

                StreamWriter sw = new StreamWriter("log" + substring[0] + ".txt", true);
                sw.WriteLine(DateTime.Now.ToString() + " " + htmlstr + " " + e.ToString());
                sw.Close();
             
            }
        }



        static bool ProxyStatusCheck(string proxyadr) //check proxy with ping
        {
            Char delimiter = ':';
            string[] substring = proxyadr.Split(delimiter);
            Ping myping = new Ping();

            try
            {
                PingReply reply = myping.Send(substring[0], 1000);

                if (reply == null)
                    return false;

                return (reply.Status == IPStatus.Success);
            }
            catch (PingException e)
            {
                return false;
            }
        }

        

        private static void CheckProxyTimerCallback(Object o)//check dead proxy, mb back to live
        {
            if (!flagStop)
            {
                string[] proxylistfromfile = null;
                if (System.IO.File.Exists("proxylist.txt"))
                    proxylistfromfile = System.IO.File.ReadAllLines("proxylist.txt");

                proxylistfromfile = CheckProxyFile(proxylistfromfile);

                WebClient cl = new WebClient();


                threadListMutex.WaitOne();


                foreach (string proxy in proxylistfromfile)
                {
                    if (flagStop)
                        break;
                    if (!threadlist.Contains(proxy))
                        try
                        {
                            WebProxy wp = new WebProxy(proxy);
                            cl.Proxy = wp;
                            cl.DownloadString("https://vk.com/id1");
                            Thread tr = new Thread(ProxyThread);
                            tr.Name = proxy;
                            tr.Start(proxy);
                            threadlist.Add(proxy);
                        }
                        catch (Exception e)
                        {

                        }

                }
                threadListMutex.ReleaseMutex();


                GC.Collect();
            }
            else
                ProxyCheckTimer.Dispose();
        }




        private static string[] CheckProxyFile(string[] proxylistfromfile)//
        {
            List<string> tmp = new List<string>();
            for (int i = 0; i < proxylistfromfile.Length; i++)
                if (!tmp.Contains(proxylistfromfile[i]))
                    tmp.Add(proxylistfromfile[i]);
            System.IO.File.WriteAllLines("proxylist.txt", tmp.ToArray());
            return tmp.ToArray();
        }
    }

    
}
