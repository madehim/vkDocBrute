using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace VkDocsBrute
{
    class Program
    {
        static int id = 1;
        static int minrange = 0;
        static int maxrange = 455000000;
        static int num = 455000000;
        static bool flagStop = false;
        static SettingsFile settingsFile = new SettingsFile();

        static List<string> skipedPages = new List<string>();
        static List<string> threadlist = new List<string>();
        static Timer ProxyCheckTimer; //check dead proxy
        static object lockNum = new object();
        static object threadListLock = new object();
        static object skipedPagesLock = new object();



        static void Main(string[] args)
        {
            if (settingsFile.IsExist())
            {
                if (settingsFile.ReadSettingsFile(out Dictionary<string, int> settings))
                {
                    id = settings["id"];
                    minrange = settings["minrange"];
                    num = settings["num"];
                }
                else
                    settingsFile.CreateOrUpdateSettingsFile(num, minrange, id);
            }
            else
            {
                settingsFile.CreateOrUpdateSettingsFile(num, minrange, id);
            }


            Console.WriteLine("Команды help, status, id, minrange, maxrange, start, stop, exit");
            string cmd = Console.ReadLine();



            while (true) //console menu stuff
            {
                switch (cmd)
                {
                    case "id":
                        if (threadlist.Count != 0)
                        {
                            Console.WriteLine("Остановите выполнение, прежде чем изменять переменные.");
                        }
                        else
                        {
                            Console.WriteLine("Текущий id " + id + ". Для изменения введите новый числовой id.");
                            if (IsInt(Console.ReadLine(), out int result))
                            {
                                id = result;
                                settingsFile.CreateOrUpdateSettingsFile(num, minrange, id);
                                Console.WriteLine("ID изменен.");
                            }
                            else
                                Console.WriteLine("Принимается только числовой ID.");
                        }
                        cmd = Console.ReadLine();
                        break;
                    case "minrange":
                        if (threadlist.Count != 0)
                        {
                            Console.WriteLine("Остановите выполнение, прежде чем изменять переменные.");
                        }
                        else
                        {
                            Console.WriteLine("Текущий minRange " + minrange + ". Для изменения введите новый числовой minRange.");
                            if (IsInt(Console.ReadLine(), out int result1))
                            {
                                minrange = result1;
                                settingsFile.CreateOrUpdateSettingsFile(num, minrange, id);
                                Console.WriteLine("minRange изменен.");
                            }
                            else
                                Console.WriteLine("Принимается только числовой minRange.");
                        }
                        cmd = Console.ReadLine();
                        break;
                    case "maxrange":
                        if (threadlist.Count != 0)
                        {
                            Console.WriteLine("Остановите выполнение, прежде чем изменять переменные.");
                        }
                        else
                        {
                            Console.WriteLine("Текущий maxRange " + num + ". Для изменения введите новый числовой maxRange.");
                            if (IsInt(Console.ReadLine(), out int result2))
                            {
                                num = result2;
                                settingsFile.CreateOrUpdateSettingsFile(num, minrange, id);
                                Console.WriteLine("maxRange изменен.");
                            }
                            else
                                Console.WriteLine("Принимается только числовой maxRange.");
                        }
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
                        Console.WriteLine();
                        Console.Write("help - эта команда \nid - смена целевого id. \nminrange - смена нижнего порога работы программы \nmaxrange - смена верхнего порога работы программы \nstatus - информация о текущих переменных или ходе работы \nstop - остановка текущей задачи \nexit - выход из приложения\n");
                        cmd = Console.ReadLine();
                        break;
                    case "threadlist":
                        foreach (string thread in threadlist)
                            Console.WriteLine(thread);
                        cmd = Console.ReadLine();
                        break;
                    case "status":
                        if (threadlist.Count != 0)
                            Console.WriteLine(DateTime.Now.ToString() + " проверено " + (maxrange - num).ToString() + " из " + (maxrange - minrange).ToString() + ", количество потоков - " + threadlist.Count + ". Текущий id " + id);
                        else
                            Console.WriteLine("ID - " + id + ", minRange - " + minrange + ", num - " + num);
                        cmd = Console.ReadLine();
                        break;
                    case "exit":
                        settingsFile.CreateOrUpdateSettingsFile(num, minrange, id);
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

        static bool IsInt(string readLine, out int result)
        {
            result = 0;
            if (int.TryParse(readLine, out int res))
            {
                result = res;
                return true;
            }
            return false;
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










        static void start(object id)
        {
            string[] proxylistfromfile = null; //get proxy list
            if (System.IO.File.Exists("proxylist.txt"))
                proxylistfromfile = System.IO.File.ReadAllLines("proxylist.txt");

            proxylistfromfile = CheckProxyFile(proxylistfromfile);

            Console.WriteLine("Proxy in file - " + proxylistfromfile.Length + ". Checking alive proxy.");



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
                    Console.WriteLine("Thread " + tr.Name + " have starting.");
                }
            }
            Thread tre = new Thread(ProxyThread); //start own pc thread
            tre.Name = "own pc";
            Console.WriteLine("Thread " + tre.Name + " have starting.");
            tre.Start("0");
            threadlist.Add(tre.Name);
            ProxyCheckTimer = new Timer(CheckProxyTimerCallback, null, 0, 1200 * proxylistfromfile.Length);

        }




        static void ProxyThread(object obj)
        {
            string htmlstr = "";
            Char delimiter = ':';
            string[] substring = obj.ToString().Split(delimiter);
            try
            {
                WebClient client = new WebClient();//start webclient with/without proxy
                WebProxy wp = new WebProxy(obj.ToString());
                if (obj.ToString() != "0")
                    client.Proxy = wp;

                wp.UseDefaultCredentials = true;//idk why its here



                while (true) //check every page with step down
                {


                    if (num == minrange && skipedPages.Count == 0) //check for end
                    {
                        if (threadlist.Count != 1)
                        {
                            threadlist.Remove(Thread.CurrentThread.Name);
                            Console.WriteLine("Thread " + Thread.CurrentThread.Name + " stopped, " + threadlist.Count + " still active");
                        }
                        else
                        {
                            threadlist.Remove(Thread.CurrentThread.Name);
                            Console.WriteLine("Last thread " + Thread.CurrentThread.Name + " stopped. All pages checked.");
                            flagStop = false;
                        }
                        break;
                    }

                    string htmlstr1 = "";
                    string strtmp = "";
                    if (skipedPages.Count != 0)//choose dec or "skiped page". second mean that some exception can happened when cheked this page before
                    {
                        lock (skipedPagesLock)
                        {
                            htmlstr1 = skipedPages.Last();
                            skipedPages.RemoveAt(skipedPages.Count - 1);
                        }
                    }
                    else
                    {
                        lock (lockNum)
                        {
                            strtmp = num.ToString();
                            num--;
                        }
                    }




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
                        if (threadlist.Count != 1)
                        {
                            lock (threadListLock)
                            {
                                threadlist.Remove(Thread.CurrentThread.Name);
                            }
                            Console.WriteLine("Thread " + Thread.CurrentThread.Name + " stopped, " + threadlist.Count + " still active");
                        }
                        else
                        {
                            lock (threadListLock)
                            {
                                threadlist.Remove(Thread.CurrentThread.Name);
                            }
                            Console.WriteLine("Last thread " + Thread.CurrentThread.Name + " stopped. ");
                            Console.WriteLine("Last number of file is " + num + " + " + skipedPages.Count + " pages were skiped.");
                            settingsFile.CreateOrUpdateSettingsFile(num, minrange, id);
                            flagStop = false;
                        }
                        break;
                    }
                }

            }
            catch (Exception e)
            {
                lock (threadListLock)
                {
                    threadlist.Remove(Thread.CurrentThread.Name);
                }

                lock (skipedPagesLock)
                {
                    skipedPages.Add(htmlstr);
                }

                Console.WriteLine("Im dead :( " + Thread.CurrentThread.Name + ". EXP " + e.Message);

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
                            lock (threadListLock)
                            {
                                threadlist.Add(proxy);
                            }
                        }
                        catch (Exception e)
                        {

                        }

                }

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
