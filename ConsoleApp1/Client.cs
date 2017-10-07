using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace FTP
{
    class Client
    {
        private String serverHostname;
        private IPAddress server;
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private IPAddress localIp;

        private bool passive = true;
        private bool debug = false;
        private char mode = 'I';
        private Regex returnCode = new Regex(@"^\d{3}\s");
        private Regex pasvPort = new Regex(@"\(\d{1,3},\d{1,3},\d{1,3},\d{1,3},\d{1,3},\d{1,3}\)");

        public Client(String host, int port)
        {
            serverHostname = host;
            try
            {
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                localIp = localIPs[2];
                client = new TcpClient(serverHostname, (port == 0) ? 21 : port);
                reader = new StreamReader(client.GetStream());
                Reader();
                writer = new StreamWriter(client.GetStream());
            }
            catch (SocketException e)
            {
                Console.Error.WriteLine("Could not connect to Host:  " + serverHostname);
                Environment.Exit(-1);
            }
        }

        private void Reader()
        {
            String line;
            do
            {
                line = reader.ReadLine();
                Console.WriteLine(line);
            } while (!returnCode.IsMatch(line));
        }

        public void Login()
        {
            Console.Write("Username: ");
            String username = Console.ReadLine();
            writer.WriteLine("USER " + username);
            writer.Flush();
            var status = reader.ReadLine().Substring(0, 1);
            if (status == "2" || status == "3")
            {
                Password();
            }
            else
            {
                Console.Error.WriteLine("There was a problem with your Login");
                Login();
            }
        }

        private void Password()
        {
            Console.Write("Password: ");
            String password = Console.ReadLine();
            writer.WriteLine("PASS " + password);  //whoops plaintext password, oh well
            writer.Flush();
            var status = reader.ReadLine().Substring(0, 1);
            if (status == "2")
            {
                Console.WriteLine("Login Success!");
            }
            else
            {
                Console.Error.WriteLine("There was a problem with your Login");
                Login();
            }
        }

        public String Passive()
        {
            passive = !passive;
            return "Transfer Mode: " + ((passive) ? "Passive" : "Active");
        }

        public void Debug()
        {
            debug = !debug;
            Console.WriteLine("Debug Mode: " + debug);
        }

        public void AccessCommand(String cmd)
        {
            if (debug) { Console.WriteLine("Send: " + cmd); }
            writer.WriteLine(cmd);
            writer.Flush();
            var status = reader.ReadLine();
            if (status.Substring(0, 1) == "2")
            {
                Console.WriteLine(status);
            }
            else
            {
                Console.Error.Write("Error:  ");
                Console.Error.WriteLine(status);
            }
        }

        public void Pwd()
        {
            AccessCommand("PWD");
        }

        public void Cdup()
        {
            AccessCommand("CDUP");
        }

        public void Cd(String path)
        {
            AccessCommand("CWD " + path);
        }

        public void Type(Char code)
        {
            AccessCommand("TYPE " + code);
        }

        public void TransferCommand(String cmd)
        {
            String status;
            if (passive)
            {
                if (debug) { Console.WriteLine("Send: PASV"); }
                writer.WriteLine("PASV");
                writer.Flush();
                status = reader.ReadLine();
                if (debug) { Console.WriteLine(status); }
                if (status.Substring(0, 1) == "2")
                {
                    try
                    {
                        Match hostResponse = pasvPort.Match(status);
                        string test = hostResponse.ToString();
                        string[] address = test.Substring(1, test.Length - 2).Split(",");
                        int port = 0;
                        port = (int.Parse(address[4]) * 256) + int.Parse(address[5]);
                        byte[] serverIp = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            serverIp[i] = Convert.ToByte(int.Parse(address[i]));
                        }
                        IPAddress dataServerAddress = new IPAddress(serverIp);
                        TcpClient dataClient = new TcpClient();
                        dataClient.Connect(dataServerAddress, port);
                        StreamReader dataStream = new StreamReader(dataClient.GetStream());
                        StreamWriter dataWriter = new StreamWriter(dataClient.GetStream());
                        if (debug) { Console.WriteLine("Send: " + cmd); }
                        writer.Write(cmd);
                        writer.Flush();
                        //Console.WriteLine(reader.ReadLine());
                        do
                        {
                            Console.WriteLine(dataStream.ReadLine());
                        } while (!dataStream.EndOfStream);
                        Console.WriteLine(reader.ReadLine());
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        Console.Error.WriteLine("Unexpected response from server");
                    }
                    catch (FormatException e)
                    {
                        Console.Error.WriteLine("Malformed Address from server");

                    }
                    catch (IndexOutOfRangeException e)
                    {
                        Console.Error.WriteLine("Malformed Address from server");
                    }
                    catch (IOException e)
                    {
                        Console.Error.WriteLine("Connection to server lost");
                        Environment.Exit(-1);
                    }

                }
                else
                {
                    Console.Error.Write("Error:  ");
                    Console.Error.WriteLine(status);
                }
            }
            else
            {
                try
                {
                    TcpListener dataListener = new TcpListener(IPAddress.Any, 0);
                    dataListener.Start();
                    int port = ((IPEndPoint)dataListener.LocalEndpoint).Port;
                    byte[] local = localIp.GetAddressBytes();
                    string hostPort = "";
                    for (int i = 0; i < 4; i++)
                    {
                        hostPort += local[i] + ",";
                    }
                    hostPort += (port - (port % 256)) / 256 + "," + port % 256;
                    if (debug) { Console.WriteLine("Send: PORT " + hostPort); }
                    writer.WriteLine("PORT " + hostPort);
                    writer.Flush();
                    status = reader.ReadLine();
                    Console.WriteLine(status);
                    if (status.Substring(0, 1) == "2")
                    {
                        TcpClient dataClient = dataListener.AcceptTcpClient();
                        StreamReader dataReader = new StreamReader(dataClient.GetStream());
                        StreamWriter dataWriter = new StreamWriter(dataClient.GetStream());
                        Console.WriteLine(dataReader.ReadLine());
                        if (debug) { Console.WriteLine("Send: " + cmd); }
                        dataWriter.Write(cmd);
                        dataWriter.Flush();
                        Console.WriteLine(reader.ReadLine());
                        String line;
                        do
                        {
                            line = dataReader.ReadLine();
                            Console.WriteLine(line);
                        } while (!line.Split()[0].StartsWith('2'));
                    }
                    else
                    {
                        Console.Error.Write("Error:  " + status);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
                
            }

        }

        public void Dir()
        {
            TransferCommand("LIST");
        }

        public void Get(String filename)
        {
            var writer = new StreamWriter(client.GetStream());
            writer.WriteLine("RETR " + filename);
            writer.Flush();

            String path = Directory.GetCurrentDirectory() + "\\" + filename;

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (FileStream fs = File.Create(path))
            {
                String line;
                byte[] file;
                do
                {
                    line = reader.ReadLine();
                    file = new UTF8Encoding(true).GetBytes(line);
                    fs.Write(file, 0, file.Length);

                } while (!reader.EndOfStream);
            }
            Console.WriteLine("Transfer Complete!");
        }

        public void Close()
        {
            reader.Close();
            writer.Close();
            client.Close();
        }

    }
}
