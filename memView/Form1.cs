using MinHook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace memView
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            gr = Graphics.FromImage(bmp);
            Text += "    PID: "+Process.GetCurrentProcess().Id;
        }
        Bitmap bmp;
        Graphics gr;

        List<long> memHistory = new List<long>();
        long max = 0;
        private void button1_Click(object sender, EventArgs e)
        {
            if (!timer1.Enabled)
            {
                proc = Process.GetProcesses().FirstOrDefault(z => z.ProcessName.ToLower().Contains(textBox1.Text));
                if (proc == null) { MessageBox.Show("not found"); }
                else
                {
                    timer1.Enabled = true;
                    memHistory.Clear();


                }
            }
            else
            {
                timer1.Enabled = false;
            }
        }

        string friendly(double len)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };

            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            string result = String.Format("{0:0.##} {1}", len, sizes[order]);
            return result;
        }
        Process proc;
        private void timer1_Tick(object sender, EventArgs e)
        {

            if (proc.HasExited)
            {
                timer1.Enabled = false;
                MessageBox.Show("process exited");
                return;
            }
            if (proc != null)
            {
                proc.Refresh();
                memHistory.Add(proc.PrivateMemorySize64);
                while (memHistory.Count > bmp.Width - 1)
                {
                    memHistory.RemoveAt(0);
                }
                label1.Text = proc.ProcessName + ": " + friendly(proc.PrivateMemorySize64) + $": max ({friendly(memHistory.Max())})    sample rate: {timer1.Interval}";
            }
            gr.Clear(Color.White);
            if (memHistory.Count > 0)
            {
                var koefy = (float)bmp.Height / memHistory.Max();
                for (int i = 0; i < memHistory.Count; i++)
                {
                    gr.DrawLine(Pens.Red, i, bmp.Height - 1, i, bmp.Height - 1 - memHistory[i] * koefy);
                }
            }


            pictureBox1.Image = bmp;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var val = int.Parse(textBox2.Text);
                if (val < 100) val = 100;
                if (val > 10000) val = 10000;
                timer1.Interval = val;
            }
            catch (Exception ex)
            {

            }
        }
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int MessageBoxW(IntPtr hWnd, String text, String caption, uint type);

        //We need to declare a delegate that matches the prototype of the hooked function
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        delegate int MessageBoxWDelegate(IntPtr hWnd, string text, string caption, uint type);
        //A variable to store the original function so that we can call
        //within our detoured MessageBoxW handler
        MessageBoxWDelegate MessageBoxW_orig;

        //Our actual detour handler function
        int MessageBoxW_Detour(IntPtr hWnd, string text, string caption, uint type)
        {
            return MessageBoxW_orig(hWnd, "HOOKED: " + text, caption, type);
        }
        HookEngine engine;
        void ChangeMessageBoxMessage()
        {

            engine = new HookEngine();


            MessageBoxW_orig = engine.CreateHook("user32.dll", "MessageBoxW", new MessageBoxWDelegate(MessageBoxW_Detour));
            //MessageBoxW_orig2 = engine.CreateHook("user32.dll", "MessageBoxW", new MessageBoxWDelegate(MessageBoxW_Detour));
            engine.EnableHooks();

            //Call the PInvoke import to test our hook is in place
            //MessageBoxW(IntPtr.Zero, "Text", "Caption", 0);



        }
        
        void ChangeMessageBoxMessage2()
        {

            if (engine == null)
                engine = new HookEngine();


            Heap_orig = engine.CreateHook("kernel32.dll", "HeapAlloc", new HeapAllocDelegate(heap_Detour));
            //MessageBoxW_orig2 = engine.CreateHook("user32.dll", "MessageBoxW", new MessageBoxWDelegate(MessageBoxW_Detour));
            engine.EnableHooks();

            //Call the PInvoke import to test our hook is in place
            //MessageBoxW(IntPtr.Zero, "Text", "Caption", 0);



        }
        private void button2_Click(object sender, EventArgs e)
        {
            ChangeMessageBoxMessage();
            MessageBox.Show(this, "dfdf", "capto", 0);

        }
        IntPtr heap_Detour(IntPtr hWnd, uint dwFlags, UIntPtr dwBytes)
        {
            return Heap_orig(hWnd, dwFlags, dwBytes);
        }
        [DllImport("kernel32.dll", SetLastError = false)]
        static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwBytes);
        //We need to declare a delegate that matches the prototype of the hooked function
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        delegate IntPtr HeapAllocDelegate(IntPtr hHeap, uint dwFlags, UIntPtr dwBytes);
        //A variable to store the original function so that we can call
        //within our detoured MessageBoxW handler
        HeapAllocDelegate Heap_orig;


        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        private void button3_Click(object sender, EventArgs e)
        {
            string dll = "user32.dll";
            string function = "MessageBoxW";
            IntPtr target = GetProcAddress(GetModuleHandle(dll), function);
            MessageBox.Show(target.ToString());
        }
        Bitmap bmp2;
        private void button4_Click(object sender, EventArgs e)
        {
            ChangeMessageBoxMessage2();
            bmp2 = new Bitmap(3000, 1500);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            int pid = int.Parse(textBox3.Text);
            Injector.Inject(pid, textBox4.Text);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int pid = int.Parse(textBox3.Text);
            Injector.Inject(pid, "2.dll");
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
