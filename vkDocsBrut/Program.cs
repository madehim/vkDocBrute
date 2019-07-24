using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using System.IO;
using System.Net.NetworkInformation;

namespace VkDocsBrute
{
    class Program
    {
        static int id = 1;
        static int minRange = 0;
        static int maxRange = 455000000;
        static int num = 455000000;
        static bool flagStop = false;
        static SettingsFile settingsFile = new SettingsFile();

        static List<string> skipedPages = new List<string>();
        static List<string> threadList = new List<string>();
        static Timer proxyCheckTimer; //check dead proxy
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
                    minRange = settings["minrange"];
                    num = settings["num"];
                }
                else
                    settingsFile.CreateOrUpdateSettingsFile(num, minRange, id);
            }
            else
                settingsFile.CreateOrUpdateSettingsFile(num, minRange, id);


            Console.WriteLine("Команды help, status, id, minrange, maxrange, start, stop, exit");
            string cmd = Console.ReadLine();



            while (true) //console menu stuff. 
            {
                switch (cmd)
                {
                    case "id":
                        if (threadList.Count != 0)
                        {
                            Console.WriteLine("Остановите выполнение, прежде чем изменять переменные.");
                        }
                        else
                        {
                            Console.WriteLine("Текущий id " + id + ". Для изменения введите новый числовой id.");
                            if (IsInt(Console.ReadLine(), out int result))
                            {
                                id = result;
                                settingsFile.CreateOrUpdateSettingsFile(num, minRange, id);
                                Console.WriteLine("ID изменен.");
                            }
                            else
                                Console.WriteLine("Принимается только числовой ID.");
                        }
                        cmd = Console.ReadLine();
                        break;
                    case "minrange":
                        if (threadList.Count != 0)
                        {
                            Console.WriteLine("Остановите выполнение, прежде чем изменять переменные.");
                        }
                        else
                        {
                            Console.WriteLine("Текущий minRange " + minRange + ". Для изменения введите новый числовой minRange.");
                            if (IsInt(Console.ReadLine(), out int result1))
                            {
                                minRange = result1;
                                settingsFile.CreateOrUpdateSettingsFile(num, minRange, id);
                                Console.WriteLine("minRange изменен.");
                            }
                            else
                                Console.WriteLine("Принимается только числовой minRange.");
                        }
                        cmd = Console.ReadLine();
                        break;
                    case "maxrange":
                        if (threadList.Count != 0)
                        {
                            Console.WriteLine("Остановите выполнение, прежде чем изменять переменные.");
                        }
                        else
                        {
                            Console.WriteLine("Текущий maxRange " + num + ". Для изменения введите новый числовой maxRange.");
                            if (IsInt(Console.ReadLine(), out int result2))
                            {
                                num = result2;
                                settingsFile.CreateOrUpdateSettingsFile(num, minRange, id);
                                Console.WriteLine("maxRange изменен.");
                            }
                            else
                                Console.WriteLine("Принимается только числовой maxRange.");
                        }
                        cmd = Console.ReadLine();
                        break;
                    case "start":
                        if (threadList.Count == 0)
                            flagStop = false;
                        if (!flagStop && threadList.Count == 0)
                        {
                            maxRange = num;
                            Thread tr = new Thread(StartBrute);
                            tr.Start(id);
                        }
                        else
                            Console.WriteLine("Подождите остановки потоков");
                        cmd = Console.ReadLine();
                        break;
                    case "help":
                        Console.WriteLine();
                        Console.Write("help - эта команда \nid - смена целевого id. \nminrange - смена нижнего порога работы программы \nmaxrange - смена верхнего порога работы программы \nstatus - информация о текущих переменных или ходе работы \nstop - остановка текущей задачи \nexit - выход из приложения\nФайл proxylist.txt в папке с приложением позволит использовать прокси. Формат файла - ip:port прокси построчно\n");
                        cmd = Console.ReadLine();
                        break;
                    case "threadlist":
                        foreach (string thread in threadList)
                            Console.WriteLine(thread);
                        cmd = Console.ReadLine();
                        break;
                    case "status":
                        if (threadList.Count != 0)
                            Console.WriteLine(DateTime.Now.ToString() + " проверено " + (maxRange - num).ToString() + " из " + (maxRange - minRange).ToString() + ", количество потоков - " + threadList.Count + ". Текущий id " + id);
                        else
                            Console.WriteLine("ID - " + id + ", minRange - " + minRange + ", num - " + num);
                        cmd = Console.ReadLine();
                        break;
                    case "exit":
                        settingsFile.CreateOrUpdateSettingsFile(num, minRange, id);
                        Environment.Exit(0);
                        break;
                    case "stop":
                        flagStop = true;
                        if (proxyCheckTimer != null)
                            proxyCheckTimer.Change(-1, Timeout.Infinite);
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



        static void StartBrute(object id)
        {
            GC.Collect(2);
            string[] proxyListFromFile = null; //get proxy list
            if (System.IO.File.Exists("proxylist.txt"))
                proxyListFromFile = System.IO.File.ReadAllLines("proxylist.txt");


            //looking for alive proxy, if list exist
            if (proxyListFromFile != null)
            {
                proxyListFromFile = CheckProxyFile(proxyListFromFile);

                Console.WriteLine("Proxy in file - " + proxyListFromFile.Length + ". Checking alive proxy.");

                string[] proxyList = new string[proxyListFromFile.Length];
                int k = 0;
                for (int i = 0; i < proxyListFromFile.Length; i++)
                {
                    if (ProxyStatusCheck(proxyListFromFile[i]))
                    {
                        proxyList[k] = proxyListFromFile[i];
                        k++;
                    }
                }
                Console.WriteLine("Alive proxy - " + (k + 1));


                //start thread with alive proxy
                for (int i = 0; i < k; i++)
                {
                    Thread tr = new Thread(ProxyThread);
                    tr.Name = proxyList[i];
                    tr.Start(proxyList[i]);
                    threadList.Add(proxyList[i]);
                    Console.WriteLine("Thread " + tr.Name + " have starting.");
                }
            }
            Thread tre = new Thread(ProxyThread); //start own pc thread
            tre.Name = "own pc";
            Console.WriteLine("Thread " + tre.Name + " have starting.");
            tre.Start("0");
            threadList.Add(tre.Name);

            if (proxyListFromFile != null)
                proxyCheckTimer = new Timer(CheckProxyTimerCallback, null, 0, 1200 * proxyListFromFile.Length);

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



                while (true) //check every page with step down
                {




                    if (num == minRange && skipedPages.Count == 0) //check for end
                    {
                        if (threadList.Count != 1)
                        {
                            threadList.Remove(Thread.CurrentThread.Name);
                            Console.WriteLine("Thread " + Thread.CurrentThread.Name + " stopped, " + threadList.Count + " still active");
                        }
                        else
                        {
                            threadList.Remove(Thread.CurrentThread.Name);
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
                            strtmp = new string('0', 9 - strtmp.Length) + strtmp;
                        htmlstr = "https://vk.com/doc" + id + "_" + strtmp;
                    }
                    else
                        htmlstr = htmlstr1;



                    string htmlCode = client.DownloadString(htmlstr);//get page


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
                        if (threadList.Count != 1)
                        {
                            lock (threadListLock)
                            {
                                threadList.Remove(Thread.CurrentThread.Name);
                            }
                            Console.WriteLine("Thread " + Thread.CurrentThread.Name + " stopped, " + threadList.Count + " still active");
                        }
                        else
                        {
                            lock (threadListLock)
                            {
                                threadList.Remove(Thread.CurrentThread.Name);
                            }
                            Console.WriteLine("Last thread " + Thread.CurrentThread.Name + " stopped. ");
                            Console.WriteLine("Last number of file is " + num + " + " + skipedPages.Count + " pages were skiped.");
                            settingsFile.CreateOrUpdateSettingsFile(num, minRange, id);
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
                    threadList.Remove(Thread.CurrentThread.Name);
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
                string[] proxyListFromFile = null;
                if (System.IO.File.Exists("proxylist.txt"))
                    proxyListFromFile = System.IO.File.ReadAllLines("proxylist.txt");

                proxyListFromFile = CheckProxyFile(proxyListFromFile);

                WebClient cl = new WebClient();



                foreach (string proxy in proxyListFromFile)
                {
                    if (flagStop)
                        break;
                    if (!threadList.Contains(proxy))
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
                                threadList.Add(proxy);
                            }
                        }
                        catch (Exception e)
                        {

                        }
                }

            }
        }




        private static string[] CheckProxyFile(string[] proxyListFromFile)//check for dublicate
        {
            proxyListFromFile = proxyListFromFile.Distinct().ToArray();
            System.IO.File.WriteAllLines("proxylist.txt", proxyListFromFile);
            return proxyListFromFile;
        }
    }


}
