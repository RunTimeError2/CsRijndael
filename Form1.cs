using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Rijndael
{
    public partial class Form1 : Form
    {
        [DllImport("Rijndael.dll", EntryPoint = "ArrayEncrypt", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ArrayEncrypt(ref byte password, int pw_length, ref byte arr_plaintext, ref byte arr_ciphertext, int arr_length);
        [DllImport("Rijndael.dll", EntryPoint = "ArrayDecrypt", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ArrayDecrypt(ref byte password, int pw_length, ref byte arr_ciphertext, ref byte arr_plaintext, int arr_length);

        public byte[] StringEncryptToArray(string password,string plaintext)
        {
            byte[] arr_pw = Encoding.UTF8.GetBytes(password);
            byte[] arr_tmp = Encoding.UTF8.GetBytes(plaintext);
            byte[] arr_plaintext;
            if (arr_tmp.Length % 16 == 0)
                arr_plaintext = arr_tmp;
            else
            {
                arr_plaintext = new byte[(arr_tmp.Length / 16 + 1) * 16];
                for (int i = 0; i < (arr_tmp.Length / 16 + 1) * 16; i++)
                    arr_plaintext[i] = (byte)((i < arr_tmp.Length) ? (arr_tmp[i]) : 0);
            }
            byte[] ans = new byte[arr_plaintext.Length];
            /*for (int i = 0; i < arr_length; i++)
                if (i < plaintext.Length)
                    arr_plaintext[i] = (byte)plaintext[i];
                else
                    arr_plaintext[i] = 0;*/
            ArrayEncrypt(ref arr_pw[0], password.Length, ref arr_plaintext[0], ref ans[0], arr_plaintext.Length);
            return ans;
        }

        public string ArrayDecryptToString(string password,byte[] ciphertext)
        {
            string ans = "";
            byte[] arr_pw = Encoding.UTF8.GetBytes(password);
            byte[] arr_plaintext = new byte[ciphertext.Length];
            ArrayDecrypt(ref arr_pw[0], password.Length, ref ciphertext[0], ref arr_plaintext[0], ciphertext.Length);
            ans = Encoding.UTF8.GetString(arr_plaintext);
            return ans;
        }

        public string ArrayToHexString(byte[] arr)
        {
            string ans = "";
            string num = "0123456789ABCDEF";
            for(int i=0;i<arr.Length;i++)
            {
                ans = ans + (char)num[arr[i] / 16] + (char)num[arr[i] % 16];
                if ((i + 1) % 16 == 0)
                    ans = ans + " ";
            }
            return ans;
        }

        public byte HexCharToByte(char ch)
        {
            if ('0' <= ch && ch <= '9')
                return (byte)(ch - 48);
            else
                return (byte)(ch - 55);
        }

        public byte[] HexStringToArray(string str)
        {
            byte[] ans = new byte[(str.Length % 33 == 0) ? (str.Length / 33 * 16) : ((str.Length / 33 + 1) * 16)];
            for(int len=0,i=0;len<str.Length;i++)
            {
                ans[i] = (byte)(16 * HexCharToByte(str[len]) + HexCharToByte(str[len + 1]));
                len += 2;
                if (len < str.Length && str[len] == ' ')
                    len++;
            }
            return ans;
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "所有文件(*.*)|*.*";
        }

        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            string plaintext, password;
            plaintext = textBox1.Text;
            password = txtPassword.Text;
            if (plaintext == "") 
            {
                MessageBox.Show("Text should not be empty");
                return;
            }
            if (password == "") 
            {
                MessageBox.Show("Password should not be empty");
                return;
            }
            byte[] ans = StringEncryptToArray(password, plaintext);
            textBox1.Text = ArrayToHexString(ans);
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show("Text should not be empty");
                return;
            }
            int count = 0;
            for (int i = 0; i < textBox1.Text.Length; i++)
                if (textBox1.Text[i] != ' ' && (textBox1.Text[i] > 'F' || textBox1.Text[i] < '0' || ('9' < textBox1.Text[i] && textBox1.Text[i] < 'A')))
                {
                    MessageBox.Show("Invalid Hex ciphertext in the textbox");
                    return;
                }
                else
                    if (textBox1.Text[i] != ' ')
                        count++;
            if (count % 16 != 0 || count == 0) 
            {
                MessageBox.Show("Invalid Hex ciphertext in the textbox");
                return;
            }
            if (txtPassword.Text == "") 
            {
                MessageBox.Show("Password should not be empty");
                return;
            }
            byte[] ciphertext = HexStringToArray(textBox1.Text);
            string ans = ArrayDecryptToString(txtPassword.Text, ciphertext);
            textBox1.Text = ans;
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            txtFilePath.Text = openFileDialog1.FileName;
        }

        private void btnFileEncrypt_Click(object sender, EventArgs e)
        {
            byte[] arr_pw;
            if (txtPassword.Text != "")
                arr_pw = Encoding.UTF8.GetBytes(txtPassword.Text);
            else
            {
                MessageBox.Show("Password should not be empty");
                return;
            }
            //==================
            FileStream fs_r = new FileStream(txtFilePath.Text, FileMode.Open);
            FileStream fs_w = new FileStream(txtFilePath.Text + ".renc", FileMode.Create);
            BinaryReader br = new BinaryReader(fs_r);
            BinaryWriter bw = new BinaryWriter(fs_w);
            bw.Seek(0, SeekOrigin.Begin);
            byte rest = (byte)(fs_r.Length % 16);
            bw.Write(rest);
            for (long i = 0; i < fs_r.Length / 16; i++)
            {
                byte[] plaintext = br.ReadBytes(16);
                byte[] ciphertext = new byte[16];
                ArrayEncrypt(ref arr_pw[0], arr_pw.Length, ref plaintext[0], ref ciphertext[0], 16);
                bw.Write(ciphertext);
            }
            if (rest != 0)
            {
                byte[] plaintext, ciphertext;
                plaintext = ciphertext = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                for (int i = 0; i < rest; i++)
                    plaintext[i] = br.ReadByte();
                ArrayEncrypt(ref arr_pw[0], arr_pw.Length, ref plaintext[0], ref ciphertext[0], 16);
                bw.Write(ciphertext);
            }
            MessageBox.Show("File successfully encrypted.");
            br.Close();
            bw.Close();
            fs_r.Close();
            fs_w.Close();

        }

        private void btnFileDecrypt_Click(object sender, EventArgs e)
        {
            byte[] arr_pw;
            if (txtPassword.Text != "")
                arr_pw = Encoding.UTF8.GetBytes(txtPassword.Text);
            else
            {
                MessageBox.Show("Password should not be empty");
                return;
            }
            //===============
            FileStream fs_r = new FileStream(txtFilePath.Text, FileMode.Open);
            FileStream fs_w;
            if (txtFilePath.Text.EndsWith(".renc"))
                fs_w = new FileStream(txtFilePath.Text.TrimEnd(".renc".ToCharArray()), FileMode.Create);
            else
                fs_w = new FileStream(txtFilePath.Text + ".rdec", FileMode.Create);
            if ((fs_r.Length - 1) % 16 != 0)
            {
                MessageBox.Show("Not valid Encrypted file.");
                fs_r.Close();
                fs_w.Close();
                return;
            }
            BinaryReader br = new BinaryReader(fs_r);
            BinaryWriter bw = new BinaryWriter(fs_w);
            bw.Seek(0, SeekOrigin.Begin);
            byte rest = br.ReadByte();
            
            for (long i = 0; i < fs_r.Length / 16; i++) 
            {
                byte[] ciphertext = br.ReadBytes(16);
                byte[] plaintext = new byte[16];
                ArrayDecrypt(ref arr_pw[0], arr_pw.Length, ref ciphertext[0], ref plaintext[0], 16);
                if(i<fs_r.Length /16-1)
                    bw.Write(plaintext);
                else
                    for (byte j = 0; j < rest; j++)
                        bw.Write(plaintext[j]);
            }
            MessageBox.Show("File successfully decrypted.");
            br.Close();
            bw.Close();
            fs_r.Close();
            fs_w.Close();
        }
        
    }
}